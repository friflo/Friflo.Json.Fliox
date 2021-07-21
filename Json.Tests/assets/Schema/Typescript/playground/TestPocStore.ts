import { Article, Order } from "../PocStore/UnitTest.Flow.Graph"


var exampleArticle: Article = {
    id: "article-id",
    name: "Article Name",
    producer: "producer-id"
}

var exampleOrder: Order = {
    id: "order-id",
    customer: "customer-id",
    items: [
        {
            amount: 1, 
            article: "article-id",
            name: "Article Name"
        }
    ]
}