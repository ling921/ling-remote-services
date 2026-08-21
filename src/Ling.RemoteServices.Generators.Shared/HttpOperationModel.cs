namespace Ling.RemoteServices.Generators;

internal sealed record HttpOperationModel(
    string Verb,
    string RelativeRoute,
    string FullRoute,
    List<ParameterModel> Parameters,
    int? SuccessStatus,
    string? ResponseContentType,
    bool IsClientDefault);
