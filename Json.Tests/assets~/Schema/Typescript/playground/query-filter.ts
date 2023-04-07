// ------------- query Filter<T> -----------------
type String = {
    /** return the length of the string. */
    readonly Length : Number,
    
    /** return **true** if the value matches the beginning of the string. */
    StartsWith  (value: string) : boolean,
    /** return **true** if the value matches the end of the string. */
    EndsWith    (value: string) : boolean,
    /** return **true** if the value occurs within the string. */
    Contains    (value: string) : boolean,
}
& (string | { })                        // remove string methods: at(), length, ...


// type Number = {} & (number | { })    // remove number methods: toFixed(), toString(), ...
type Number = Pick<number, "valueOf">   // remove number methods: toFixed(), toString(), ... picked most useless method


export const PI:   Number = 3.141592653589793;
export const E:    Number = 2.718281828459045;
export const Tau:  Number = 6.283185307179586;

export function Abs    (value: number | Number) : Number { return 0; }
export function Log    (value: number | Number) : Number { return 0; }
export function Exp    (value: number | Number) : Number { return 0; }
export function Sqrt   (value: number | Number) : Number { return 0; }
export function Floor  (value: number | Number) : Number { return 0; }
export function Ceiling(value: number | Number) : Number { return 0; }

/** Scalar object property like string, number or boolean. */
type Property = string | number | boolean;

type List<T> = {
    /** return the length of the array. */
    readonly Length : Number,
    /** return **true** if *all* the elements in a sequence satisfy the filter condition. */
    All     (filter: (o: T) => boolean) : boolean;
    /** return **true** if *any* element in a sequence satisfy the filter condition. */
    Any     (filter: (o: T) => boolean) : boolean;

    /** return the minimum value of an array. */
    Min     (filter: (o: T) => Property) : Number;
    /** return the maximum value of an array. */
    Max     (filter: (o: T) => Property) : Number;
    /** return the sum of all values. */
    Sum     (filter: (o: T) => Property) : Number;
    /** return the average of all values. */
    Average (filter: (o: T) => Property) : Number;

    /** count the elements in an array which satisfy the filter condition. */
    Count   (filter: (o: T) => boolean)  : Number;
}

type FilterTypes<T> =
    T extends string         ? String   : 
    T extends number         ? Number   : 
//  T extends Array<infer U> ? List<U>
    T extends (infer U)[]    ? List<U>                  // alternative for: Array<infer U>
    : Filter<T>

export type Filter<T> = {
    readonly [K in keyof T]-? : FilterTypes<T[K]> & { } // type NonNullable<T> = T & {};
}
