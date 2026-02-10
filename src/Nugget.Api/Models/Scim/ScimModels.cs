using System.Text.Json.Serialization;

namespace Nugget.Api.Models.Scim;

public class ScimListResponse<T>
{
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = ["urn:ietf:params:scim:api:messages:2.0:ListResponse"];
    
    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }
    
    [JsonPropertyName("itemsPerPage")]
    public int ItemsPerPage { get; set; }
    
    [JsonPropertyName("startIndex")]
    public int StartIndex { get; set; }
    
    [JsonPropertyName("Resources")]
    public List<T> Resources { get; set; } = new();
}

public class ScimUser
{
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = ["urn:ietf:params:scim:schemas:core:2.0:User"];
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("externalId")]
    public string? ExternalId { get; set; }
    
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;
    
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;
    
    [JsonPropertyName("active")]
    public bool Active { get; set; }
    
    [JsonPropertyName("emails")]
    public List<ScimEmail> Emails { get; set; } = new();
    
    [JsonPropertyName("groups")]
    public List<ScimGroupRef> Groups { get; set; } = new();
    
    [JsonPropertyName("meta")]
    public ScimMeta Meta { get; set; } = new();
}

public class ScimEmail
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "work";
    
    [JsonPropertyName("primary")]
    public bool Primary { get; set; } = true;
}

public class ScimGroup
{
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = ["urn:ietf:params:scim:schemas:core:2.0:Group"];
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;
    
    [JsonPropertyName("members")]
    public List<ScimMember> Members { get; set; } = new();
    
    [JsonPropertyName("meta")]
    public ScimMeta Meta { get; set; } = new();
}

public class ScimMember
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
    
    [JsonPropertyName("display")]
    public string? Display { get; set; }
}

public class ScimGroupRef
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("display")]
    public string? Display { get; set; }
}

public class ScimMeta
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = string.Empty;
    
    [JsonPropertyName("created")]
    public string Created { get; set; } = string.Empty;
    
    [JsonPropertyName("lastModified")]
    public string LastModified { get; set; } = string.Empty;
    
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
}

public class ScimError
{
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = ["urn:ietf:params:scim:api:messages:2.0:Error"];
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;
}

public class ScimPatchRequest
{
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = ["urn:ietf:params:scim:api:messages:2.0:PatchOp"];
    
    [JsonPropertyName("Operations")]
    public List<ScimPatchOperation> Operations { get; set; } = new();
}

public class ScimPatchOperation
{
    [JsonPropertyName("op")]
    public string Op { get; set; } = string.Empty; // add, remove, replace
    
    [JsonPropertyName("path")]
    public string? Path { get; set; }
    
    [JsonPropertyName("value")]
    public object? Value { get; set; } // Can be dictionary or list
}
