export const filterSource =
`
// ------------- query Filter<T> -----------------
type StringFilter = {
    /** return the length of the string. */
    readonly Length : Number,
    
    /** return **true** if the value matches the beginning of the string. */
    StartsWith  (value: string) : boolean,
    /** return **true** if the value matches the end of the string. */
    EndsWith    (value: string) : boolean,
    /** return **true** if the value occurs within the string. */
    Contains    (value: string) : boolean,
}                        

type String = StringFilter & (string | { }); // remove string methods: at(), length, ...

// type Number = {} & (number | { })    // remove number methods: toFixed(), toString(), ...
type Number = Pick<number, "valueOf">   // remove number methods: toFixed(), toString(), ... picked most useless method


// todo - should export math constants with namespace Math
/** Represents the ratio of the circumference of a circle to its diameter, specified by the constant, π. */
export const PI:   Number = 3.141592653589793;
/** Represents the natural logarithmic base, specified by the constant, e. */
export const E:    Number = 2.718281828459045;
/** Represents the number of radians in one turn, specified by the constant, τ. */
export const Tau:  Number = 6.283185307179586;

// todo - should export math methods with namespace Math
/** Returns the absolute value of the given number. */
export declare function Abs    (value: number | Number) : Number;
/** Returns the logarithm of the given number */
export declare function Log    (value: number | Number) : Number;
/** Returns e raised to the power of the given number. */
export declare function Exp    (value: number | Number) : Number;
/** Returns the square root of the given number. */
export declare function Sqrt   (value: number | Number) : Number;
/** Returns the largest integral value less than or equal to the given number. */
export declare function Floor  (value: number | Number) : Number;
/** Returns the smallest integral value greater than or equal to the given number. */
export declare function Ceiling(value: number | Number) : Number;

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
    T extends (infer U)[]    ? List<U>  // alternative for: Array<infer U>
    : Filter<T & { }>                   // type NonNullable<T> = T & {};

export type Filter<T> = {
    readonly [K in keyof T]-? : FilterTypes<T[K]>
}

export type FilterExpression<T> = (o: Filter<T>) => boolean
`;