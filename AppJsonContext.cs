using System.Text.Json.Serialization;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;
using CrmWebApi.DTOs.Audit;
using CrmWebApi.DTOs.Auth;
using CrmWebApi.DTOs.Department;
using CrmWebApi.DTOs.Drug;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.OrgType;
using CrmWebApi.DTOs.Phys;
using CrmWebApi.DTOs.Policy;
using CrmWebApi.DTOs.Spec;
using CrmWebApi.DTOs.User;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi;

[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(AccessTokenResponse))]
[JsonSerializable(typeof(PendingConfirmationResponse))]
[JsonSerializable(typeof(PagedResponse<UserResponse>))]
[JsonSerializable(typeof(PagedResponse<OrgResponse>))]
[JsonSerializable(typeof(PagedResponse<PhysResponse>))]
[JsonSerializable(typeof(PagedResponse<ActivResponse>))]
[JsonSerializable(typeof(PagedResponse<DrugResponse>))]
[JsonSerializable(typeof(PagedResponse<DepartmentResponse>))]
[JsonSerializable(typeof(PagedResponse<AuditLogResponse>))]
[JsonSerializable(typeof(AuditLogResponse))]
[JsonSerializable(typeof(DepartmentResponse))]
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(OrgResponse))]
[JsonSerializable(typeof(PhysResponse))]
[JsonSerializable(typeof(ActivResponse))]
[JsonSerializable(typeof(DrugResponse))]
[JsonSerializable(typeof(OrgTypeResponse))]
[JsonSerializable(typeof(PolicyResponse))]
[JsonSerializable(typeof(SpecResponse))]
[JsonSerializable(typeof(IEnumerable<OrgTypeResponse>))]
[JsonSerializable(typeof(IEnumerable<PolicyResponse>))]
[JsonSerializable(typeof(IEnumerable<SpecResponse>))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
internal partial class AppJsonContext : JsonSerializerContext;
