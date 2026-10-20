## Part 0 — Setup and baseline

Ran the starter API and tested it in Swagger UI. Two bad behaviors reproduced:

1. **Invalid input is accepted.**
   `POST /api/Product` with `{ "name": "", "sku": "string", "price": -100, "salePrice": 0 }`
   returned **200 OK** and stored the product (id 3). An empty name and a negative
   price were accepted because there is no validation.

2. **A missing resource causes a server error.**
   `GET /api/Product/9999` threw an unhandled
   `System.InvalidOperationException` ("Sequence contains no matching element")
   and returned **500 Internal Server Error** instead of a clean **404 Not Found**


----
## Part 1 — Data Annotations validation

Added `CreateProductRequest` and `UpdateProductRequest` with `[Required]`, `[StringLength]`
and `[Range]`. `[ApiController]` is on the controller, so `ModelState` is checked automatically.

`POST /api/Product` with `{ "name": "", "sku": "string", "price": -100, "salePrice": 0 }`
now returns **400 Bad Request**:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": [
      "The Name field is required.",
      "The field Name must be a string with a minimum length of 2 and a maximum length of 100."
    ],
    "Price": [
      "The field Price must be between 0.01 and 1000000."
    ],
    "SalePrice": [
      "The field SalePrice must be between 0.01 and 1000000."
    ]
  },
  "traceId": "00-926500da804c288a9a69283f69278d16-8b52f9998e11d239-00"
}
```

------

## Part 2 — Custom rule and domain exception

**Custom rule:** `CreateProductRequest` implements `IValidatableObject`. `SalePrice` must be
less than `Price`, a cross-property rule that attributes can't express. `UpdateProductRequest`
inherits it, so the rule lives in one place.

Message returned: **"SalePrice must be less than Price."**

`POST /api/Product` with `{ "name": "Lamp", "sku": "LP-1", "price": 10, "salePrice": 15 }`
returns **400 Bad Request**:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "SalePrice": [
      "SalePrice must be less than Price."
    ]
  },
  "traceId": "00-4a33bdac75837d9ac25ea6b3dc95a3ac-50442d4c7ac00d44-00"
}
```

**Domain exception:** `GetById`, `Update` and `Delete` use a `FindOrThrow` helper that throws
`WebApplication1.Exceptions.NotFoundException` ("Product 9999 was not found.") when the id
doesn't exist. With `UseExceptionHandler()` commented out, `GET /api/Product/9999` returned
**500 Internal Server Error**: the response body exposed the full stack trace, local file
paths and request headers.


------
## Part 3 — Global exception handling

**Handler:** `GlobalExceptionHandler : IExceptionHandler` maps `NotFoundException` to 404,
`ConflictException` to 409 and anything else to 500.

**Registration (Program.cs):** `AddExceptionHandler<GlobalExceptionHandler>()`,
`AddProblemDetails()` and `app.UseExceptionHandler()`.

**Logging:** the full exception is logged server-side with `_logger.LogError`, and only a
safe `ProblemDetails` body is returned to the client.

`GET /api/Product/9999` (a missing id) now returns **404 Not Found**:

```json
{
  "title": "Resource not found",
  "status": 404,
  "detail": "Product 9999 was not found."
}
```

Matching server log line (full detail stays on the server):

```
WebApplication1.Middleware.GlobalExceptionHandler: Error: Unhandled exception (404)

WebApplication1.Exceptions.NotFoundException: Product 9999 was not found.
   at WebApplication1.Controllers.ProductController.FindOrThrow(Int32 id) in ...\Controllers\ProductController.cs:line 19
   at WebApplication1.Controllers.ProductController.GetById(Int32 id) in ...\Controllers\ProductController.cs:line 26
   ...
```

**Expected result:** Met. The missing id returns a clean 404 with a `ProblemDetails` body
instead of the raw 500 from Part 2. The stack trace, file paths and headers no longer reach
the client; the full detail appears only in the server log. For any exception that is not
`NotFoundException` or `ConflictException`, the handler returns 500 with no `detail`.

**Checkpoint:** the 404 body and the matching server log line are shown above.

----
## Part 4 — Standardized responses

- `POST` returns **201 Created** with a `Location` header, `DELETE` returns **204 No Content**,
  and `GET` / `PUT` return **200**.
- A duplicate `Sku` throws `ConflictException`, which the global handler maps to **409**.
- All errors (validation, not found, conflict, server) come back as Problem Details.
- The list endpoint supports `page` and `pageSize` and returns a `data` / `meta` envelope.
  Single items are returned directly.

**201 Created** (`POST /api/Product` with `{ "name": "Lamp", "sku": "LP-1", "price": 10, "salePrice": 8 }`):

```
location: https://localhost:7072/api/Product/4
```
```json
{
  "id": 4,
  "name": "Lamp",
  "sku": "LP-1",
  "price": 10,
  "salePrice": 8
}
```

**404 Not Found** (`GET /api/Product/9999`):

```json
{
  "title": "Resource not found",
  "status": 404,
  "detail": "Product 9999 was not found."
}
```

**Paged list** (`GET /api/Product?page=1&pageSize=2`):

```json
{
  "data": [
    { "id": 1, "name": "Keyboard", "sku": "KB-001", "price": 49.99, "salePrice": null },
    { "id": 2, "name": "Mouse", "sku": "MS-001", "price": 19.99, "salePrice": null }
  ],
  "meta": {
    "page": 1,
    "pageSize": 2,
    "totalCount": 4,
    "totalPages": 2
  }
}
```

**409 Conflict** (the same POST sent a second time):

```json
{
  "title": "Conflict",
  "status": 409,
  "detail": "A product with SKU 'LP-1' already exists."
}
```

**204 No Content:** `DELETE /api/Product/4` returned 204 with no response body.

**Expected result:** Met. Single items are returned directly, the collection is a paged
envelope, and every error is Problem Details with the correct status code.

**Checkpoint:** a 201, a 404 and the paged list are shown above.

---

## Reflection

Validation, exception handling and standard responses each handle a different stage of a
request, and together they make the API predictable. Validation rejects bad input at the
boundary with a 400 before it reaches any logic, while domain exceptions like
`NotFoundException` and `ConflictException` let the code say what went wrong without knowing
anything about HTTP. The global handler then turns those exceptions into the right status
codes in one place, logging the full detail server-side and returning only a safe body.
Because