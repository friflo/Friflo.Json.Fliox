import { Article, Order, TestType, PocStore } from "../PocStore/UnitTest.Fliox.Client"

/*
type UnArray<T> = T extends Array<infer U> ? U : T;

type RecursiveObject<T> = T extends Array<T> ? never : UnArray<UnArray<T>>

export type Filter<TModel> = {
    [Key in keyof TModel]-? : TModel[Key] extends RecursiveObject<TModel[Key]>  // -? declare all fields required
        ? Filter<TModel[Key]>                                                   // convert nested fields recursive
        : List<NonNullable<UnArray<TModel[Key]>>>;                              // convert T[] -> List<T>, NonNullable<> remove null | undefined from array type
}; */

// type Without<T, K> = Pick<T, Exclude<keyof T, K>>;
// type Str = Exclude<string, "length"> | { Length: number };

// ------------- query Filter<T> -----------------
const PI:   number = 3.141592653589793;
const E:    number = 2.718281828459045;
const Tau:  number = 6.283185307179586;

function Abs    (value: number) : number { return 0; }
function Log    (value: number) : number { return 0; }
function Exp    (value: number) : number { return 0; }
function Sqrt   (value: number) : number { return 0; }
function Floor  (value: number) : number { return 0; }
function Ceiling(value: number) : number { return 0; }

/** Scalar object property like string, number or boolean. */
type Property = string | number | boolean;

type List<T> = {
    /** Return the length of the array. */
    readonly Length : number,
    /** Return **true** if *all* the elements in a sequence satisfy the filter condition. */
    All     (filter: (o: T) => boolean) : boolean;
    /** Return **true** if *any* element in a sequence satisfy the filter condition. */
    Any     (filter: (o: T) => boolean) : boolean;

    /** Return the minimum value of an array. */
    Min     (filter: (o: T) => Property) : number;
    /** Return the maximum value of an array. */
    Max     (filter: (o: T) => Property) : number;
    /** Return the sum of all values. */
    Sum     (filter: (o: T) => Property) : number;
    /** Return the average of all values. */
    Average (filter: (o: T) => Property) : number;

    /** Counts the elements in an array which satisfy the filter condition. */
    Count   (filter: (o: T) => boolean) : number;
}

type StringFilter = {
    /** Return the length of the string. */
    readonly Length : number,
    
    /** Return **true** if the value matches the beginning of the string. */
    StartsWith  (value: string) : boolean,
    /** Return **true** if the value matches the end of the string. */
    EndsWith    (value: string) : boolean,
    /** Return **true** if the value occurs within the string. */
    Contains    (value: string) : boolean,
}

type FilterTypes<T> =
    T extends string         ? StringFilter & (string | { }) : // remove string methods: at(), length, ...
    T extends number         ? number | { }                  : // remove Number methods: toFixed(), toString(), ...
    T extends Array<infer U> ? List<U>
    : Filter<T>

export type Filter<T> = {
    readonly [K in keyof T]-? : FilterTypes<T[K]> & { }     // type NonNullable<T> = T & {};
}

// ------------- query Filter<T> ----------------- end

function query<T>(filter: (o: FilterTypes<T>) => boolean) { }

query(o => true);

// --- ensure presence string filter methods
query<Article>(o => o.id.Length == 3);
query<Article>(o => o.id.StartsWith("abc"));
query<Article>(o => o.id.EndsWith  ("abc"));
query<Article>(o => o.id.Contains  ("abc"));

// --- ensure absence of standard string & number methods
// @ts-expect-error : Property 'length' does not exist on type 'StringFilter'.
query<Article>(o => o.id.length == 3);  // expect error!
// @ts-expect-error : Property 'at' does not exist on type 'StringFilter'.
query<Article>(o => o.id.at(1) == "d"); // expect error!
// @ts-expect-error : Property 'toFixed' does not exist on type 'number | {}'.
query<TestType>(o => o.int32.toFixed() == 1);

// --- ensure presence standard scalar operator
query<TestType> (o => o.derivedClass.amount == 1);
query<Order>    (o => o.customer == "dddd");
query<Order>    (o => o.customer != "dddd");
query<TestType> (o => o.int32 == 1);

// --- ensure presence List<> / array filter methods
query<Order>(o => o.items.Length == 1);

query<Order>(o => o.items.Any(o => o.amount == 1));
query<Order>(o => o.items.All(o => o.amount == 1));

query<Order>(o => o.items.Min     (o => o.amount)           == 1);
query<Order>(o => o.items.Max     (o => o.amount)           == 2);
query<Order>(o => o.items.Sum     (o => o.amount)           == 6);
query<Order>(o => o.items.Average (o => o.amount)           == 3);

query<Order>(o => o.items.Count   (o => o.name == "Camera") == 2);

query<Order>(o => Abs(o.items.Length) == 1);

type EntityType = PocStore["articles"][string]