import { RuleExpression } from './rule-model';

/**
 * A small formula language over the rule tree: text in, the same AST the server already evaluates out, and
 * back again.
 *
 * WHY TEXT AT ALL. The visual tree is faithful to how SuccessFactors draws a rule, but it makes the common
 * case slow -- a two-condition rule costs a dozen clicks through nested dropdowns, and a nested formula like
 * AddCalendarDays(JoiningDate, 100) is genuinely painful to assemble. Typing it is faster for anyone who can
 * type, and an autocomplete list makes it just as discoverable as a dropdown. This is the direction every
 * comparable product went: Excel formulas, Salesforce formula fields, JQL.
 *
 * WHY IT LIVES ENTIRELY IN THE BROWSER. The parser emits the exact JSON the server has always stored, so
 * nothing on the server changes -- no new endpoint, no second grammar to keep in step with the evaluator,
 * and rules authored in the tree open in the formula box and vice versa. Text is a VIEW of the rule, never
 * the stored form. The server keeps validating the tree it receives, so a parser bug cannot widen what a
 * rule is allowed to do.
 *
 * ROUND-TRIPPING is the property that makes this safe to ship alongside the tree editor: print(parse(x))
 * must equal x for every rule, or switching editors would quietly rewrite somebody's rule. The date literal
 * syntax (#2026-01-01#) exists only for that reason -- without it a Date constant would come back as a
 * String and the stored JSON would drift on every open.
 */

// -------------------------------------------------------------------------------------------------
// Tokens
// -------------------------------------------------------------------------------------------------

type TokenType = 'identifier' | 'number' | 'string' | 'date' | 'operator' | 'keyword' | 'punct' | 'eof';

interface Token {
    type: TokenType;
    value: string;
    /** Offset of the token's first character, used to place caret-aware autocomplete and error markers. */
    start: number;
    end: number;
}

const KEYWORDS = ['AND', 'OR', 'NOT', 'IN', 'IS', 'EMPTY', 'CONTAINS', 'STARTS', 'ENDS', 'WITH', 'TRUE', 'FALSE'];

/** Longest first: <= must win over <, and <> over <. */
const OPERATORS = ['<=', '>=', '<>', '!=', '==', '=', '<', '>', '+', '-', '*', '/', '%'];

export class RuleFormulaError extends Error {
    constructor(
        message: string,
        readonly position: number
    ) {
        super(message);
        this.name = 'RuleFormulaError';
    }
}

function tokenize(text: string): Token[] {
    const tokens: Token[] = [];
    let i = 0;

    while (i < text.length) {
        const ch = text[i];

        if (/\s/.test(ch)) {
            i++;
            continue;
        }

        // Date literal: #2026-01-01#. Exists purely so Date constants survive a print/parse round trip.
        if (ch === '#') {
            const close = text.indexOf('#', i + 1);

            if (close < 0) {
                throw new RuleFormulaError('A date literal is missing its closing #.', i);
            }

            tokens.push({ type: 'date', value: text.substring(i + 1, close), start: i, end: close + 1 });
            i = close + 1;
            continue;
        }

        if (ch === '"' || ch === "'") {
            const quote = ch;
            let j = i + 1;
            let value = '';

            while (j < text.length && text[j] !== quote) {
                // Backslash escapes so a rule can contain a literal quote.
                if (text[j] === '\\' && j + 1 < text.length) {
                    value += text[j + 1];
                    j += 2;
                    continue;
                }

                value += text[j];
                j++;
            }

            if (j >= text.length) {
                throw new RuleFormulaError('A text value is missing its closing quote.', i);
            }

            tokens.push({ type: 'string', value, start: i, end: j + 1 });
            i = j + 1;
            continue;
        }

        if (/[0-9]/.test(ch)) {
            let j = i;
            while (j < text.length && /[0-9.]/.test(text[j])) {
                j++;
            }

            tokens.push({ type: 'number', value: text.substring(i, j), start: i, end: j });
            i = j;
            continue;
        }

        if (/[A-Za-z_]/.test(ch)) {
            let j = i;
            // Dots are part of the identifier so "Employee.JoiningDate" is one token: the autocomplete needs
            // to see the whole path to know it should be offering fields rather than parameters.
            while (j < text.length && /[A-Za-z0-9_.]/.test(text[j])) {
                j++;
            }

            const raw = text.substring(i, j);
            const isKeyword = KEYWORDS.indexOf(raw.toUpperCase()) >= 0 && raw.indexOf('.') < 0;

            tokens.push({ type: isKeyword ? 'keyword' : 'identifier', value: raw, start: i, end: j });
            i = j;
            continue;
        }

        if (ch === '(' || ch === ')' || ch === ',') {
            tokens.push({ type: 'punct', value: ch, start: i, end: i + 1 });
            i++;
            continue;
        }

        const operator = OPERATORS.find((op) => text.startsWith(op, i));

        if (operator) {
            tokens.push({ type: 'operator', value: operator, start: i, end: i + operator.length });
            i += operator.length;
            continue;
        }

        throw new RuleFormulaError(`Unexpected character "${ch}".`, i);
    }

    tokens.push({ type: 'eof', value: '', start: text.length, end: text.length });

    return tokens;
}

// -------------------------------------------------------------------------------------------------
// Parser
// -------------------------------------------------------------------------------------------------

/**
 * Recursive descent, lowest precedence first: OR, AND, NOT, comparison, additive, multiplicative, unary,
 * primary. The order below IS the precedence table -- reading the call chain downward tells you how a
 * formula binds, which is why it is written as one function per level rather than a table.
 */
class Parser {
    private position = 0;

    constructor(private readonly tokens: Token[]) {}

    static parse(text: string): RuleExpression {
        const parser = new Parser(tokenize(text));
        const expression = parser.parseOr();

        parser.expect('eof');

        return expression;
    }

    private get current(): Token {
        return this.tokens[this.position];
    }

    private advance(): Token {
        return this.tokens[this.position++];
    }

    private matchKeyword(...words: string[]): boolean {
        return this.current.type === 'keyword' && words.indexOf(this.current.value.toUpperCase()) >= 0;
    }

    private matchOperator(...operators: string[]): boolean {
        return this.current.type === 'operator' && operators.indexOf(this.current.value) >= 0;
    }

    private expect(type: TokenType, value?: string): Token {
        if (this.current.type !== type || (value !== undefined && this.current.value !== value)) {
            const found = this.current.type === 'eof' ? 'the end of the formula' : `"${this.current.value}"`;
            throw new RuleFormulaError(`Expected ${value ?? type} but found ${found}.`, this.current.start);
        }

        return this.advance();
    }

    private parseOr(): RuleExpression {
        let left = this.parseAnd();

        while (this.matchKeyword('OR')) {
            this.advance();
            const right = this.parseAnd();

            // Flattened rather than nested: A OR B OR C is one junction box with three operands, which is
            // both how the tree editor draws it and how the author thinks about it.
            left =
                left.kind === 'logical' && left.op === 'Or'
                    ? { ...left, operands: [...(left.operands ?? []), right] }
                    : { kind: 'logical', op: 'Or', operands: [left, right] };
        }

        return left;
    }

    private parseAnd(): RuleExpression {
        let left = this.parseNot();

        while (this.matchKeyword('AND')) {
            this.advance();
            const right = this.parseNot();

            left =
                left.kind === 'logical' && left.op === 'And'
                    ? { ...left, operands: [...(left.operands ?? []), right] }
                    : { kind: 'logical', op: 'And', operands: [left, right] };
        }

        return left;
    }

    private parseNot(): RuleExpression {
        if (this.matchKeyword('NOT')) {
            this.advance();
            return { kind: 'not', operand: this.parseNot() };
        }

        return this.parseComparison();
    }

    private parseComparison(): RuleExpression {
        const left = this.parseAdditive();

        // IS EMPTY / IS NOT EMPTY -- the unary operators, which take no right-hand side.
        if (this.matchKeyword('IS')) {
            this.advance();

            let negated = false;

            if (this.matchKeyword('NOT')) {
                this.advance();
                negated = true;
            }

            this.expectKeyword('EMPTY');

            return { kind: 'compare', op: negated ? 'IsNotEmpty' : 'IsEmpty', left };
        }

        if (this.matchKeyword('NOT')) {
            // Only "NOT IN" is valid here; a bare NOT after an operand is a mistake worth naming.
            const save = this.position;
            this.advance();

            if (this.matchKeyword('IN')) {
                this.advance();
                return { kind: 'compare', op: 'NotIn', left, right: this.parseList() };
            }

            this.position = save;
            return left;
        }

        if (this.matchKeyword('IN')) {
            this.advance();
            return { kind: 'compare', op: 'In', left, right: this.parseList() };
        }

        if (this.matchKeyword('CONTAINS')) {
            this.advance();
            return { kind: 'compare', op: 'Contains', left, right: this.parseAdditive() };
        }

        if (this.matchKeyword('STARTS', 'ENDS')) {
            const which = this.advance().value.toUpperCase();
            this.expectKeyword('WITH');

            return {
                kind: 'compare',
                op: which === 'STARTS' ? 'StartsWith' : 'EndsWith',
                left,
                right: this.parseAdditive(),
            };
        }

        if (this.matchOperator('=', '==', '<>', '!=', '<', '<=', '>', '>=')) {
            const operator = this.advance().value;

            return {
                kind: 'compare',
                op: COMPARISON_BY_SYMBOL[operator],
                left,
                right: this.parseAdditive(),
            };
        }

        return left;
    }

    private parseAdditive(): RuleExpression {
        let left = this.parseMultiplicative();

        while (this.matchOperator('+', '-')) {
            const operator = this.advance().value;

            left = {
                kind: 'arith',
                op: operator === '+' ? 'Add' : 'Subtract',
                left,
                right: this.parseMultiplicative(),
            };
        }

        return left;
    }

    private parseMultiplicative(): RuleExpression {
        let left = this.parsePrimary();

        while (this.matchOperator('*', '/', '%')) {
            const operator = this.advance().value;

            left = {
                kind: 'arith',
                op: operator === '*' ? 'Multiply' : operator === '/' ? 'Divide' : 'Modulo',
                left,
                right: this.parsePrimary(),
            };
        }

        return left;
    }

    private parseList(): RuleExpression {
        this.expect('punct', '(');

        const items: RuleExpression[] = [];

        if (this.current.value !== ')') {
            items.push(this.parseOr());

            while (this.current.value === ',') {
                this.advance();
                items.push(this.parseOr());
            }
        }

        this.expect('punct', ')');

        return { kind: 'list', items };
    }

    private parsePrimary(): RuleExpression {
        const token = this.current;

        if (token.type === 'punct' && token.value === '(') {
            this.advance();
            const inner = this.parseOr();
            this.expect('punct', ')');

            return inner;
        }

        if (token.type === 'number') {
            this.advance();

            return {
                kind: 'const',
                dataType: token.value.indexOf('.') >= 0 ? 'Decimal' : 'Integer',
                value: token.value,
            };
        }

        if (token.type === 'string') {
            this.advance();
            return { kind: 'const', dataType: 'String', value: token.value };
        }

        if (token.type === 'date') {
            this.advance();
            return { kind: 'const', dataType: 'Date', value: token.value };
        }

        if (this.matchKeyword('TRUE', 'FALSE')) {
            const value = this.advance().value.toLowerCase();
            return { kind: 'const', dataType: 'Boolean', value };
        }

        if (token.type === 'operator' && token.value === '-') {
            // Unary minus, expressed as 0 - x so the AST needs no negation node.
            this.advance();

            return {
                kind: 'arith',
                op: 'Subtract',
                left: { kind: 'const', dataType: 'Integer', value: '0' },
                right: this.parsePrimary(),
            };
        }

        if (token.type === 'identifier') {
            this.advance();

            // Identifier followed by "(" is a call; containing "." is a field; otherwise a variable. That
            // three-way split is why the tokenizer keeps dots inside identifiers.
            if (this.current.type === 'punct' && this.current.value === '(') {
                const args = this.parseList();
                return { kind: 'fn', name: token.value, args: args.items ?? [] };
            }

            const dot = token.value.indexOf('.');

            if (dot > 0) {
                return {
                    kind: 'field',
                    parameter: token.value.substring(0, dot),
                    field: token.value.substring(dot + 1),
                };
            }

            return { kind: 'var', name: token.value };
        }

        const found = token.type === 'eof' ? 'the end of the formula' : `"${token.value}"`;
        throw new RuleFormulaError(`Expected a value but found ${found}.`, token.start);
    }

    private expectKeyword(word: string): void {
        if (!this.matchKeyword(word)) {
            const found = this.current.type === 'eof' ? 'the end of the formula' : `"${this.current.value}"`;
            throw new RuleFormulaError(`Expected ${word} but found ${found}.`, this.current.start);
        }

        this.advance();
    }
}

const COMPARISON_BY_SYMBOL: Record<string, string> = {
    '=': 'Equal',
    '==': 'Equal',
    '<>': 'NotEqual',
    '!=': 'NotEqual',
    '<': 'LessThan',
    '<=': 'LessThanOrEqual',
    '>': 'GreaterThan',
    '>=': 'GreaterThanOrEqual',
};

const SYMBOL_BY_COMPARISON: Record<string, string> = {
    Equal: '=',
    NotEqual: '<>',
    LessThan: '<',
    LessThanOrEqual: '<=',
    GreaterThan: '>',
    GreaterThanOrEqual: '>=',
};

const SYMBOL_BY_ARITHMETIC: Record<string, string> = {
    Add: '+',
    Subtract: '-',
    Multiply: '*',
    Divide: '/',
    Modulo: '%',
};

/** Text to AST. Throws {@link RuleFormulaError} with a caret position the editor can point at. */
export function parseFormula(text: string): RuleExpression {
    if (!text || !text.trim()) {
        throw new RuleFormulaError('The formula is empty.', 0);
    }

    return Parser.parse(text);
}

/** Text to AST, or undefined for a blank formula. Used where a condition is optional. */
export function parseOptionalFormula(text: string): RuleExpression | undefined {
    return text && text.trim() ? parseFormula(text) : undefined;
}

// -------------------------------------------------------------------------------------------------
// Printer
// -------------------------------------------------------------------------------------------------

/**
 * AST to text. Parentheses are emitted from precedence, not from the original text, so a rule built in the
 * tree editor prints as something an author would have typed -- and reparses to the identical tree.
 */
export function printFormula(expression: RuleExpression | null | undefined): string {
    return expression ? print(expression, 0) : '';
}

/** Precedence levels, matching the parser's call chain. Higher binds tighter. */
const PRECEDENCE = {
    or: 1,
    and: 2,
    not: 3,
    compare: 4,
    additive: 5,
    multiplicative: 6,
    primary: 7,
};

function print(expression: RuleExpression, parentPrecedence: number): string {
    switch (expression.kind) {
        case 'const':
            return printConstant(expression);

        case 'field':
            return `${expression.parameter}.${expression.field}`;

        case 'var':
            return expression.name ?? '';

        case 'fn':
            return `${expression.name}(${(expression.args ?? []).map((a) => print(a, 0)).join(', ')})`;

        case 'list':
            return `(${(expression.items ?? []).map((i) => print(i, 0)).join(', ')})`;

        case 'not':
            return wrap(`NOT ${print(expression.operand!, PRECEDENCE.not)}`, PRECEDENCE.not, parentPrecedence);

        case 'logical': {
            const keyword = expression.op === 'Or' ? 'OR' : 'AND';
            const precedence = expression.op === 'Or' ? PRECEDENCE.or : PRECEDENCE.and;
            const parts = (expression.operands ?? []).map((o) => print(o, precedence));

            return wrap(parts.join(` ${keyword} `), precedence, parentPrecedence);
        }

        case 'compare':
            return wrap(printComparison(expression), PRECEDENCE.compare, parentPrecedence);

        case 'arith': {
            const precedence =
                expression.op === 'Add' || expression.op === 'Subtract'
                    ? PRECEDENCE.additive
                    : PRECEDENCE.multiplicative;

            const symbol = SYMBOL_BY_ARITHMETIC[expression.op ?? ''] ?? '?';
            const text = `${print(expression.left!, precedence)} ${symbol} ${print(expression.right!, precedence + 1)}`;

            return wrap(text, precedence, parentPrecedence);
        }

        default:
            return '';
    }
}

function printComparison(expression: RuleExpression): string {
    const left = print(expression.left!, PRECEDENCE.compare);

    switch (expression.op) {
        case 'IsEmpty':
            return `${left} IS EMPTY`;
        case 'IsNotEmpty':
            return `${left} IS NOT EMPTY`;
        case 'In':
            return `${left} IN ${print(expression.right!, 0)}`;
        case 'NotIn':
            return `${left} NOT IN ${print(expression.right!, 0)}`;
        case 'Contains':
            return `${left} CONTAINS ${print(expression.right!, PRECEDENCE.compare)}`;
        case 'StartsWith':
            return `${left} STARTS WITH ${print(expression.right!, PRECEDENCE.compare)}`;
        case 'EndsWith':
            return `${left} ENDS WITH ${print(expression.right!, PRECEDENCE.compare)}`;
        default:
            return `${left} ${SYMBOL_BY_COMPARISON[expression.op ?? ''] ?? '='} ${print(expression.right!, PRECEDENCE.compare)}`;
    }
}

function printConstant(expression: RuleExpression): string {
    const value = expression.value ?? '';

    switch (expression.dataType) {
        case 'Integer':
        case 'Decimal':
            // A blank numeric constant would print as nothing and reparse as a syntax error, so it prints
            // as a quoted empty string instead -- visible to the author rather than silently broken.
            return value === '' ? '""' : value;

        case 'Boolean':
            return value.toLowerCase() === 'true' ? 'true' : 'false';

        case 'Date':
            return `#${value}#`;

        default:
            return `"${value.replace(/\\/g, '\\\\').replace(/"/g, '\\"')}"`;
    }
}

function wrap(text: string, precedence: number, parentPrecedence: number): string {
    return precedence < parentPrecedence ? `(${text})` : text;
}
