import { Article, Order, TestType, PocStore, Producer } from "../PocStore/UnitTest.Fliox.Client"
import { FilterExpression, PI, E, Tau, Abs, Log, Exp, Sqrt, Floor, Ceiling } from "./query-filter";

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


/** some IntClass docs */
export class IntClass {
    /** some num docs */
    num : number;
}

function query<T>(filter: FilterExpression<T>) { }

query(o => true);

// ------------ number
// --- ensure presence number operators
query<IntClass> (o => o.num  == 3);
query<IntClass> (o => o.num  != 3);
query<IntClass> (o => o.num  == null);

// --- ensure absence of standard number methods
// @ts-expect-error : Property 'toFixed' does not exist on type 'Number'.
query<TestType> (o => o.int32.toFixed() == 1);
// @ts-expect-error :  This comparison appears to be unintentional because the types 'Number' and 'string' have no overlap
query<IntClass> (o => o.num  == "3");

// ------------ string
// --- ensure presence string filter methods
query<Article>  (o => o.id  == "abc");
query<Article>  (o => o.id  == null);
query<Article>  (o => o.id.Length == 3);
query<Article>  (o => o.id.StartsWith("abc"));
query<Article>  (o => o.id.EndsWith  ("abc"));
query<Article>  (o => o.id.Contains  ("abc"));

// --- ensure absence of standard string methods
// @ts-expect-error : Property 'length' does not exist on type String & {}'.
query<Article>  (o => o.id.length == 3);  // expect error!
// @ts-expect-error : Property 'at' does not exist on type 'String & {}'.
query<Article>  (o => o.id.at(1) == "d"); // expect error!

// ------------ operators
// --- ensure presence standard scalar operator
query<TestType> (o => o.derivedClass.amount == 1);
query<Order>    (o => o.customer == "dddd");
query<Order>    (o => o.customer != "dddd");
query<TestType> (o => o.int32 == 1);

// ------------ List<>
// --- ensure presence List<> / array filter methods
query<Order>    (o => o.items.Length == 1);

query<Order>    (o => o.items.Any(o => o.amount == 1));
query<Order>    (o => o.items.All(o => o.amount == 1));

query<Order>    (o => o.items.Min     (o => o.amount)           == 1);
query<Order>    (o => o.items.Max     (o => o.amount)           == 2);
query<Order>    (o => o.items.Sum     (o => o.amount)           == 6);
query<Order>    (o => o.items.Average (o => o.amount)           == 3);

query<Order>    (o => o.items.Count   (o => o.name == "Camera") == 2);
query<Order>    (o => o.items.Count   () == 2);


// ------------ Math
// --- ensure presence of Math methods
query<IntClass> (o => Abs(42) == 1);
query<IntClass> (o => Log(o.num) == 1);
query<IntClass> (o => Exp(o.num) == 1);
query<IntClass> (o => Sqrt(o.num) == 1);
query<IntClass> (o => Floor(o.num) == 1);
query<IntClass> (o => Ceiling(o.num) == 1);


type EntityType = PocStore["articles"][string]