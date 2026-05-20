# API

Base URL: `http://localhost:5000`

All endpoints use JSON request and response bodies.

Authenticated citizen endpoints expect:

```http
Authorization: Bearer <token>
```

The token is returned by `POST /api/v1/citizens/login` and is valid for 7 days.

## Error Format

Validation errors return `400 Bad Request`:

```json
{
  "code": "validation_error",
  "message": "Rule name is required.",
  "details": null
}
```

Missing or invalid citizen token returns `401 Unauthorized`.

Missing resource returns `404 Not Found`.

## Data Flow

1. Create organizations with `POST /api/v1/organizations`.
2. Create target documents with `POST /api/v1/target-documents` and attach organization ids to them.
3. Create rules with `POST /api/v1/rules`. A rule points to one target document and contains one or more matching profiles.
4. Reference endpoints expose stay purposes and citizenships that were created while creating rules or citizens.
5. A citizen registers and logs in.
6. The citizen can update profile properties with `PUT /api/v1/citizens/me`.
7. The citizen creates a roadmap request with `POST /api/v1/citizens/me/roadmaps`.
8. The backend matches the citizen against rules by citizenship, stay purpose, days since entry date, and optional profile properties.
9. The matched roadmap returns the required target document, organizations, guidance, deadline metrics, and status.

## Organizations

Organizations are places where a citizen can obtain or process a target document.

Target documents reference organizations by id. Rules reference target documents, so organizations are indirectly shown in matched citizen roadmaps.

### `GET /api/v1/organizations`

Returns all organizations ordered by name.

Response `200 OK`:

```json
[
  {
    "id": 1,
    "name": "Medical Center",
    "address": "Main street, 1"
  }
]
```

### `POST /api/v1/organizations`

Creates an organization.

Request:

```json
{
  "name": "Medical Center",
  "address": "Main street, 1"
}
```

Response `201 Created`:

```json
{
  "id": 1,
  "name": "Medical Center",
  "address": "Main street, 1"
}
```

### `PUT /api/v1/organizations/{id}`

Updates an organization.

Request:

```json
{
  "name": "Updated Medical Center",
  "address": "Main street, 2"
}
```

Response `200 OK`:

```json
{
  "id": 1,
  "name": "Updated Medical Center",
  "address": "Main street, 2"
}
```

Returns `404 Not Found` if the organization does not exist.

## Target Documents

Target documents describe what document or action is required by a rule. Each target document must be linked to at least one organization.

### `GET /api/v1/target-documents`

Returns all target documents ordered by name.

Response `200 OK`:

```json
[
  {
    "id": 1,
    "name": "Medical examination",
    "organizations": [
      {
        "id": 1,
        "name": "Medical Center",
        "address": "Main street, 1"
      }
    ]
  }
]
```

### `POST /api/v1/target-documents`

Creates a target document and links it to organizations.

Request:

```json
{
  "name": "Medical examination",
  "organizationIds": [1]
}
```

Response `201 Created`:

```json
{
  "id": 1,
  "name": "Medical examination",
  "organizations": [
    {
      "id": 1,
      "name": "Medical Center",
      "address": "Main street, 1"
    }
  ]
}
```

Returns `400 Bad Request` if `name` is empty, `organizationIds` is empty, or at least one organization id does not exist.

### `PUT /api/v1/target-documents/{id}`

Updates a target document and replaces its organization links.

Request:

```json
{
  "name": "Updated medical examination",
  "organizationIds": [1, 2]
}
```

Response `200 OK`:

```json
{
  "id": 1,
  "name": "Updated medical examination",
  "organizations": [
    {
      "id": 1,
      "name": "Medical Center",
      "address": "Main street, 1"
    }
  ]
}
```

Returns `404 Not Found` if the target document does not exist.

## Rules

Rules describe which target document and guidance apply to a group of citizens.

A rule contains profiles. A profile matches when:

- `stayDays` is greater than or equal to the citizen's days in the country.
- `stayPurposes` contains the requested stay purpose.
- `citizenships` contains the citizen's citizenship.
- every rule profile property exists in the citizen profile properties with the same name and value.

If multiple profiles match, the backend picks the profile with the smallest suitable `stayDays`, then the one with more properties, then the rule with the smallest id.

### `POST /api/v1/rules`

Creates a rule. Missing stay purposes, citizenships, and roadmap version are created automatically by name.

Request:

```json
{
  "name": "Medical examination for work",
  "roadmapVersion": "v1",
  "targetDocumentId": 1,
  "guidance": {
    "description": "Pass the medical examination."
  },
  "profiles": [
    {
      "stayDays": 30,
      "stayPurposes": ["WORK"],
      "citizenships": ["KZ"],
      "properties": [
        {
          "name": "isHighQualifiedSpecialist",
          "value": "true"
        }
      ]
    }
  ]
}
```

Response `201 Created`:

```json
{
  "id": 1,
  "name": "Medical examination for work",
  "roadmapVersion": "v1",
  "guidance": {
    "description": "Pass the medical examination."
  },
  "targetDocument": {
    "id": 1,
    "name": "Medical examination",
    "organizations": []
  },
  "profiles": [
    {
      "stayDays": 30,
      "stayPurposes": ["WORK"],
      "citizenships": ["KZ"],
      "properties": [
        {
          "name": "isHighQualifiedSpecialist",
          "value": "true"
        }
      ]
    }
  ]
}
```

Returns `400 Bad Request` if required fields are missing or `targetDocumentId` does not exist.

### `GET /api/v1/rules`

Returns all rules ordered by id.

Optional query parameters:

- `roadmapVersion` filters rules by roadmap version.

Example:

```http
GET /api/v1/rules?roadmapVersion=v1
```

Response `200 OK`:

```json
[
  {
    "id": 1,
    "name": "Medical examination for work",
    "roadmapVersion": "v1",
    "guidance": {
      "description": "Pass the medical examination."
    },
    "targetDocument": {
      "id": 1,
      "name": "Medical examination",
      "organizations": []
    },
    "profiles": []
  }
]
```

### `GET /api/v1/rules/{id}`

Returns one rule by id.

Response `200 OK` has the same shape as a single item from `GET /api/v1/rules`.

Returns `404 Not Found`:

```json
{
  "code": "not_found",
  "message": "Rule was not found.",
  "details": {
    "id": 123
  }
}
```

## Reference Data

Reference endpoints are read-only. Values appear here after they are created through rules or citizen registration/update.

### `GET /api/v1/reference/stay-purposes`

Returns stay purposes ordered by name.

Response `200 OK`:

```json
[
  {
    "id": 1,
    "name": "WORK"
  }
]
```

### `GET /api/v1/reference/citizenships`

Returns citizenships ordered by name.

Response `200 OK`:

```json
[
  {
    "id": 1,
    "name": "KZ"
  }
]
```

## Citizen Account

Citizen account endpoints handle registration, login, profile data, and roadmap history.

The authenticated profile is resolved from the bearer token.

### `POST /api/v1/citizens/register`

Registers a citizen. The citizenship is created automatically if it does not exist.

Request:

```json
{
  "fullName": "Ivan Ivanov",
  "email": "ivan@example.com",
  "password": "password",
  "citizenshipName": "KZ"
}
```

Response `201 Created`:

```json
{
  "id": 1,
  "fullName": "Ivan Ivanov",
  "email": "ivan@example.com",
  "citizenshipName": "KZ",
  "properties": []
}
```

Returns `400 Bad Request` if required fields are missing or a citizen with the same normalized email already exists.

### `POST /api/v1/citizens/login`

Creates a citizen session and returns a bearer token.

Request:

```json
{
  "email": "ivan@example.com",
  "password": "password"
}
```

Response `200 OK`:

```json
{
  "token": "session-token",
  "citizen": {
    "id": 1,
    "fullName": "Ivan Ivanov",
    "email": "ivan@example.com",
    "citizenshipName": "KZ",
    "properties": []
  }
}
```

Returns `401 Unauthorized` if email or password is incorrect.

### `GET /api/v1/citizens/me`

Returns the currently authenticated citizen.

Response `200 OK`:

```json
{
  "id": 1,
  "fullName": "Ivan Ivanov",
  "email": "ivan@example.com",
  "citizenshipName": "KZ",
  "properties": [
    {
      "name": "isHighQualifiedSpecialist",
      "value": "true"
    }
  ]
}
```

### `PUT /api/v1/citizens/me`

Updates the currently authenticated citizen.

All fields are optional:

- `fullName` updates the name when provided and not blank.
- `citizenshipName` updates or creates the citizenship when provided and not blank.
- `properties` replaces all citizen profile properties when provided.

Request:

```json
{
  "fullName": "Ivan Petrov",
  "citizenshipName": "KZ",
  "properties": [
    {
      "name": "isHighQualifiedSpecialist",
      "value": "true"
    }
  ]
}
```

Response `200 OK`:

```json
{
  "id": 1,
  "fullName": "Ivan Petrov",
  "email": "ivan@example.com",
  "citizenshipName": "KZ",
  "properties": [
    {
      "name": "isHighQualifiedSpecialist",
      "value": "true"
    }
  ]
}
```

### `POST /api/v1/citizens/me/roadmaps`

Creates a roadmap request for the authenticated citizen and stores it in history.

The request uses the citizen's current citizenship from the profile. `citizenshipName` exists in the request model but is not used by the matching logic.

Request:

```json
{
  "entryDate": "2026-05-01T00:00:00Z",
  "stayPurposeName": "WORK"
}
```

Response `200 OK` when a rule is matched:

```json
{
  "id": 1,
  "citizenId": 1,
  "entryDate": "2026-05-01T00:00:00Z",
  "stayPurpose": "WORK",
  "citizenship": "KZ",
  "ruleId": 1,
  "status": "matched",
  "message": "Date of entry and deadline information.",
  "stayDays": 30,
  "daysPassed": 18,
  "daysRemaining": 12,
  "deadlineDate": "2026-05-31T00:00:00Z",
  "isOverdue": false,
  "targetDocument": {
    "id": 1,
    "name": "Medical examination",
    "organizations": []
  },
  "guidance": {
    "description": "Pass the medical examination."
  }
}
```

Response `200 OK` when no rule is matched:

```json
{
  "id": 2,
  "citizenId": 1,
  "entryDate": "2026-05-01T00:00:00Z",
  "stayPurpose": "WORK",
  "citizenship": "KZ",
  "ruleId": null,
  "status": "not_found",
  "message": "Suitable rule was not found.",
  "stayDays": null,
  "daysPassed": null,
  "daysRemaining": null,
  "deadlineDate": null,
  "isOverdue": false,
  "targetDocument": null,
  "guidance": null
}
```

Returns `400 Bad Request` if `entryDate` is missing, is in the future, or `stayPurposeName` is empty.

### `GET /api/v1/citizens/me/roadmaps`

Returns roadmap request history for the authenticated citizen, newest first.

Response `200 OK`:

```json
[
  {
    "id": 1,
    "citizenId": 1,
    "entryDate": "2026-05-01T00:00:00Z",
    "stayPurpose": "WORK",
    "citizenship": "KZ",
    "ruleId": 1,
    "status": "matched",
    "message": "Date of entry and deadline information.",
    "stayDays": 30,
    "daysPassed": 18,
    "daysRemaining": 12,
    "deadlineDate": "2026-05-31T00:00:00Z",
    "isOverdue": false,
    "targetDocument": {
      "id": 1,
      "name": "Medical examination",
      "organizations": []
    },
    "guidance": {
      "description": "Pass the medical examination."
    }
  }
]
```
