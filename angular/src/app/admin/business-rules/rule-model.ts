import { RuleDataType } from '@shared/service-proxies/service-proxies';

/**
 * The client-side mirror of the rule tree the server parses.
 *
 * Deliberately plain interfaces rather than generated proxy types: the definition travels as a JSON STRING
 * in `DefinitionJson`, so NSwag never sees its shape and there is nothing to generate. The property names
 * here are the wire contract with `RuleDefinitionSerializer` -- change one and stored rules stop loading.
 *
 * Key order does not matter. The server reads through JsonNode precisely so that the builder can serialise
 * its component state in whatever order it likes.
 */

export type RuleExpressionKind = 'const' | 'field' | 'var' | 'fn' | 'compare' | 'logical' | 'not' | 'arith' | 'list';

export interface RuleExpression {
    kind: RuleExpressionKind;

    // const
    dataType?: keyof typeof RuleDataType | string;
    value?: string;

    // field / set target
    parameter?: string;
    field?: string;

    // var / fn
    name?: string;
    args?: RuleExpression[];

    // compare / logical / arith
    op?: string;
    left?: RuleExpression;
    right?: RuleExpression;
    operands?: RuleExpression[];
    operand?: RuleExpression;
    items?: RuleExpression[];
}

export interface RuleAction {
    kind: 'set' | 'error';

    // set
    parameter?: string;
    field?: string;
    value?: RuleExpression;

    // error
    messageKey?: string;
    args?: RuleExpression[];
}

export interface RuleBlock {
    if?: RuleExpression | null;
    then: RuleAction[];
    else?: RuleAction[];
}

export interface RuleVariable {
    name: string;
    dataType: string;
    value: RuleExpression;
}

export interface RuleDefinition {
    version: number;
    variables?: RuleVariable[];
    blocks: RuleBlock[];
}

export const COMPARISON_OPERATORS = [
    { value: 'Equal', key: 'RuleOperatorEqual' },
    { value: 'NotEqual', key: 'RuleOperatorNotEqual' },
    { value: 'GreaterThan', key: 'RuleOperatorGreaterThan' },
    { value: 'GreaterThanOrEqual', key: 'RuleOperatorGreaterThanOrEqual' },
    { value: 'LessThan', key: 'RuleOperatorLessThan' },
    { value: 'LessThanOrEqual', key: 'RuleOperatorLessThanOrEqual' },
    { value: 'Contains', key: 'RuleOperatorContains' },
    { value: 'StartsWith', key: 'RuleOperatorStartsWith' },
    { value: 'EndsWith', key: 'RuleOperatorEndsWith' },
    { value: 'IsEmpty', key: 'RuleOperatorIsEmpty' },
    { value: 'IsNotEmpty', key: 'RuleOperatorIsNotEmpty' },
];

/** Operators that take no right-hand operand. The editor hides the value box for these. */
export const UNARY_OPERATORS = ['IsEmpty', 'IsNotEmpty'];

export const ARITHMETIC_OPERATORS = [
    { value: 'Add', symbol: '+' },
    { value: 'Subtract', symbol: '−' },
    { value: 'Multiply', symbol: '×' },
    { value: 'Divide', symbol: '÷' },
    { value: 'Modulo', symbol: '%' },
];

export function emptyDefinition(): RuleDefinition {
    return {
        version: 1,
        variables: [],
        blocks: [newBlock()],
    };
}

export function newBlock(): RuleBlock {
    return {
        if: newComparison(),
        then: [],
        else: [],
    };
}

export function newComparison(): RuleExpression {
    return {
        kind: 'compare',
        op: 'Equal',
        left: newConstant(),
        right: newConstant(),
    };
}

export function newConstant(dataType = 'String'): RuleExpression {
    return { kind: 'const', dataType, value: '' };
}

export function newLogical(op: 'And' | 'Or'): RuleExpression {
    return { kind: 'logical', op, operands: [newComparison(), newComparison()] };
}

/**
 * Strips the empty collections the editor keeps for convenience, so a saved definition contains only what
 * the author actually built. It matters beyond tidiness: an empty `else` round-trips as a block with no
 * actions, which the server rejects.
 */
export function pruneDefinition(definition: RuleDefinition): RuleDefinition {
    return {
        version: definition.version || 1,
        variables: (definition.variables || []).filter((v) => !!v.name),
        blocks: (definition.blocks || []).map((block) => {
            const pruned: RuleBlock = {
                then: block.then || [],
            };

            if (block.if) {
                pruned.if = block.if;
            }

            if (block.else && block.else.length > 0) {
                pruned.else = block.else;
            }

            return pruned;
        }),
    };
}
