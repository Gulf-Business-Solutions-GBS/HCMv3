import { Component, input, output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { AppComponentBase } from '@shared/common/app-component-base';
import { RuleBaseObjectMetadataDto, RuleFunctionMetadataDto } from '@shared/service-proxies/service-proxies';
import {
    ARITHMETIC_OPERATORS,
    COMPARISON_OPERATORS,
    RuleExpression,
    UNARY_OPERATORS,
    newComparison,
    newConstant,
    newLogical,
} from './rule-model';

/**
 * Edits one node of a rule's expression tree, recursing into itself for children.
 *
 * The shape on screen follows the SuccessFactors rule editor -- an `and` / `or` junction box on the left
 * with its conditions stacked to the right -- because that is the layout configurers coming from that
 * product already know how to read. What it renders is exactly the tree the server evaluates; there is no
 * intermediate text form, so nothing can be lost in translation between the picture and the behaviour.
 *
 * Recursion in a component template needs the component to reference itself in `imports`, which Angular
 * permits for standalone components and is the whole reason this is a separate component rather than a
 * block inside the modal.
 */
@Component({
    selector: 'rule-expression-editor',
    templateUrl: './rule-expression-editor.component.html',
    imports: [FormsModule, LocalizePipe],
})
export class RuleExpressionEditorComponent extends AppComponentBase {
    readonly expression = input.required<RuleExpression>();
    readonly baseObject = input<RuleBaseObjectMetadataDto | undefined>(undefined);
    readonly functions = input<RuleFunctionMetadataDto[]>([]);

    /** True for the tree root, where removing the node would leave the block with no condition at all. */
    readonly isRoot = input<boolean>(false);

    readonly remove = output<void>();
    readonly changed = output<void>();

    readonly comparisonOperators = COMPARISON_OPERATORS;
    readonly arithmeticOperators = ARITHMETIC_OPERATORS;

    isUnary(op?: string): boolean {
        return !!op && UNARY_OPERATORS.indexOf(op) >= 0;
    }

    /** Every "Parameter.Field" the current base object exposes, for the field picker. */
    fieldOptions(): { parameter: string; field: string; label: string }[] {
        const baseObject = this.baseObject();

        if (!baseObject) {
            return [];
        }

        const options: { parameter: string; field: string; label: string }[] = [];

        for (const parameter of baseObject.parameters) {
            for (const field of parameter.fields) {
                options.push({
                    parameter: parameter.name,
                    field: field.name,
                    label: parameter.displayName + '.' + field.displayName,
                });
            }
        }

        return options;
    }

    fieldKey(expression: RuleExpression): string {
        return expression.parameter && expression.field ? expression.parameter + '.' + expression.field : '';
    }

    setFieldFromKey(expression: RuleExpression, key: string): void {
        const separator = key.indexOf('.');

        if (separator < 0) {
            return;
        }

        expression.parameter = key.substring(0, separator);
        expression.field = key.substring(separator + 1);
        this.changed.emit();
    }

    /**
     * Switches a node to a different kind in place, seeding the fields that kind needs. Mutating rather
     * than replacing keeps the parent's reference valid, which is what lets the recursion stay simple.
     */
    changeKind(expression: RuleExpression, kind: string): void {
        expression.kind = kind as any;

        switch (kind) {
            case 'const':
                expression.dataType = expression.dataType || 'String';
                expression.value = expression.value || '';
                break;

            case 'field':
                expression.parameter = expression.parameter || '';
                expression.field = expression.field || '';
                break;

            case 'fn':
                expression.name = expression.name || (this.functions()[0]?.name ?? '');
                expression.args = expression.args || [];
                this.syncFunctionArgs(expression);
                break;

            case 'compare':
                expression.op = expression.op || 'Equal';
                expression.left = expression.left || newConstant();
                expression.right = expression.right || newConstant();
                break;

            case 'logical':
                expression.op = expression.op || 'And';
                expression.operands = expression.operands || [newComparison(), newComparison()];
                break;

            case 'not':
                expression.operand = expression.operand || newComparison();
                break;

            case 'arith':
                expression.op = expression.op || 'Add';
                expression.left = expression.left || newConstant('Integer');
                expression.right = expression.right || newConstant('Integer');
                break;
        }

        this.changed.emit();
    }

    onFunctionChanged(expression: RuleExpression): void {
        this.syncFunctionArgs(expression);
        this.changed.emit();
    }

    /**
     * Grows or trims the argument list to match the chosen function's signature, so the editor cannot post
     * a call the server will reject for arity. Existing arguments are kept where the positions still line
     * up -- swapping AddCalendarDays for AddMonths should not clear the date the author already picked.
     */
    private syncFunctionArgs(expression: RuleExpression): void {
        const descriptor = this.functions().find((f) => f.name === expression.name);

        if (!descriptor) {
            return;
        }

        expression.args = expression.args || [];

        const required = descriptor.parameters.filter((p) => !p.isOptional).length;

        while (expression.args.length < required) {
            expression.args.push(newConstant());
        }

        if (expression.args.length > descriptor.parameters.length) {
            expression.args.length = descriptor.parameters.length;
        }
    }

    functionParameterName(expression: RuleExpression, index: number): string {
        const descriptor = this.functions().find((f) => f.name === expression.name);

        return descriptor?.parameters[index]?.name ?? '';
    }

    addOperand(expression: RuleExpression): void {
        expression.operands = expression.operands || [];
        expression.operands.push(newComparison());
        this.changed.emit();
    }

    removeOperand(expression: RuleExpression, index: number): void {
        expression.operands?.splice(index, 1);
        this.changed.emit();
    }

    wrapInGroup(expression: RuleExpression, op: 'And' | 'Or'): void {
        const wrapped = newLogical(op);

        // The existing node becomes the first operand rather than being discarded -- an author clicking
        // "group" means "add a condition alongside this one", never "start over".
        wrapped.operands = [JSON.parse(JSON.stringify(expression)), newComparison()];

        Object.keys(expression).forEach((key) => delete (expression as any)[key]);
        Object.assign(expression, wrapped);

        this.changed.emit();
    }
}
