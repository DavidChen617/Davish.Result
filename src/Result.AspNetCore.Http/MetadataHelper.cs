using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;

namespace Davish.Result;

/// <summary>
/// Invokes a built-in <c>Microsoft.AspNetCore.Http.HttpResults</c> type's <see cref="IEndpointMetadataProvider"/>
/// implementation. The interface's static member is implemented explicitly on those types, so it can only be
/// reached through a generic constraint, not by naming the type directly (e.g. <c>Ok.PopulateMetadata(...)</c>
/// does not compile).
/// </summary>
internal static class MetadataHelper
{
    public static void PopulateFrom<T>(MethodInfo method, EndpointBuilder builder) where T : IEndpointMetadataProvider =>
        T.PopulateMetadata(method, builder);
}
