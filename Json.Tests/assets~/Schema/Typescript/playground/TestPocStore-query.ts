import { Article, Order, TestType } from "../PocStore/UnitTest.Fliox.Client"

type List<T> = {
    readonly Length : number,

    Any  (filter: (o: T) => boolean) : boolean;
    All  (filter: (o: T) => boolean) : boolean;

    Min  (filter: (o: T) => string | number | boolean) : number;
    Max  (filter: (o: T) => string | number | boolean) : number;
    Sum  (filter: (o: T) => string | number | boolean) : number;

    Count(filter: (o: T) => boolean) : number;
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

type FilterTypes<T> =
    T extends string ? string & {
        readonly Length : number,
                 StartsWith  (str: string) : boolean,
                 EndsWith    (str: string) : boolean,
                 Contains    (str: string) : boolean,
    } :
    T extends Array<infer U>    ? List<U>
    : Filter<T>

type Filter<T> = {
    readonly [K in keyof T]-? : FilterTypes<T[K]> & { }     // type NonNullable<T> = T & {};
}

function query<T>(filter: (o: FilterTypes<T>) => boolean) { }

query(o => true);

query<Article>(o => o.id.Length == 3);
query<Article>(o => o.id.StartsWith("abc"));
query<Article>(o => o.id.EndsWith("abc"));
query<Article>(o => o.id.Contains("abc"));

query<Article>(o => o.id.at(1) == "ddd"); // expect error

query<TestType>(o => o.derivedClass.amount == 1);

query<Order>(o => o.customer == "dddd");
query<Order>(o => o.items.Length == 1);

query<Order>(o => o.items.Any   (o => o.amount == 1));
query<Order>(o => o.items.All   (o => o.amount == 1));

query<Order>(o => o.items.Min   (o => o.amount) == 1);
query<Order>(o => o.items.Max   (o => o.amount) == 2);
query<Order>(o => o.items.Sum   (o => o.amount) == 6);

query<Order>(o => o.items.Count (o => o.name == "Camera") == 2);