// // ============================================================
// // FILE: Models/CommitmentMapper.cs
// // ============================================================
// namespace Procore.APIClasses;

// public class CommitmentContractMapper
// {
//     public static CommitmentContractRequest ToPatchRequest(
//         WorkOrderContractResponse response)
//     {
//         WorkOrderContractData data = response.Data;

//         return new CommitmentContractRequest
//         {
//             Number = data.Number,
//             Status = data.Status,
//             Executed = data.Executed,
//             Title = data.Title,
//             Description = data.Description,
//             RetainagePercent = data.RetainagePercent.ToString(),
//             VendorId = ParseInt(data.Vendor?.Id),
//             AssigneeId = ParseInt(data.Assignee?.Id),
//             Private = data.Private,
//             CustomFields = data.CustomFields != null
//                 ? new Dictionary<string, object>(data.CustomFields)
//                 : null
//         };
//     }

//     private static int? ParseInt(string? value)
//         => int.TryParse(value, out var i) ? i : null;
// }

// // ============================================================
// // END FILE: Models/CommitmentMapper.cs
// // ============================================================