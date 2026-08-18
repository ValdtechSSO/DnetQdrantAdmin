namespace Dnet.QdrantAdmin.Api.Infrastructure.Models;

public class HttpClientOptions
{
    /// <summary>
    /// Accept certificates whose chain failed only because the certificate revocation
    /// status could not be determined (OCSP/CRL unreachable on some networks).
    /// Trust itself is still validated. Enable it when embedding calls fail with
    /// "RevocationStatusUnknown" while curl/browsers work.
    /// </summary>
    public bool IgnoreCertificateRevocationErrors { get; set; }
}
