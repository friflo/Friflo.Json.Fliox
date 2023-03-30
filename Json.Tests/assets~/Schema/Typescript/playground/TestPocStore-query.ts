import { Article, Order, TestType } from "../PocStore/UnitTest.Fliox.Client"

const PI:   number = 3.141592653589793;
const E:    number = 2.718281828459045;
const Tau:  number = 6.283185307179586;

function Abs    (value: number) : number { return 0; }
function Log    (value: number) : number { return 0; }
function Exp    (value: number) : number { return 0; }
function Sqrt   (value: number) : number { return 0; }
function Floor  (value: number) : number { return 0; }
function Ceiling(value: number) : number { return 0; }

type List<T> = {
    readonly Length : number,

    Any     (filter: (o: T) => boolean) : boolean;
    All     (filter: (o: T) => boolean) : boolean;

    Min     (filter: (o: T) => string | number | boolean) : number;
    Max     (filter: (o: T) => string | number | boolean) : number;
    Sum     (filter: (o: T) => string | number | boolean) : number;
    Average (filter: (o: T) => string | number | boolean) : number;

    Count   (filter: (o: T) => boolean) : number;
}

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

type StringFilter = {
    readonly Length : number,
             StartsWith  (str: string) : boolean,
             EndsWith    (str: string) : boolean,
             Contains    (str: string) : boolean,
}

type FilterTypes<T> =
    T extends string         ? StringFilter & (string | { }) :
    T extends Array<infer U> ? List<U>
    : Filter<T>

type Filter<T> = {
    readonly [K in keyof T]-? : FilterTypes<T[K]> & { }     // type NonNullable<T> = T & {};
}

function query<T>(filter: (o: FilterTypes<T>) => boolean) { }

query(o => true);

query<Article>(o => o.id.Length == 3);
query<Article>(o => o.id.StartsWith("abc"));
query<Article>(o => o.id.EndsWith  ("abc"));
query<Article>(o => o.id.Contains  ("abc"));

// @ts-expect-error : Property 'length' does not exist on type 'StringFilter'.
query<Article>(o => o.id.length == 3);  // expect error!
// @ts-expect-error : Property 'at' does not exist on type 'StringFilter'.
query<Article>(o => o.id.at(1) == "d"); // expect error!

query<TestType>(o => o.derivedClass.amount == 1);

query<Order>(o => o.customer == "dddd");
query<Order>(o => o.customer != "dddd");

query<Order>(o => o.items.Length == 1);

query<Order>(o => o.items.Any(o => o.amount == 1));
query<Order>(o => o.items.All(o => o.amount == 1));

query<Order>(o => o.items.Min     (o => o.amount)           == 1);
query<Order>(o => o.items.Max     (o => o.amount)           == 2);
query<Order>(o => o.items.Sum     (o => o.amount)           == 6);
query<Order>(o => o.items.Average (o => o.amount)           == 3);

query<Order>(o => o.items.Count   (o => o.name == "Camera") == 2);

query<Order>(o => Abs(o.items.Length) == 1);