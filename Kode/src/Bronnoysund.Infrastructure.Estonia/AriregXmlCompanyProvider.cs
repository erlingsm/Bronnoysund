// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Bronnoysund.Application.Dtos;
using Bronnoysund.Application.Ports;
using Bronnoysund.Application.Results;
using Bronnoysund.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bronnoysund.Infrastructure.Estonia;

/// <summary>
/// Adapter for <see cref="ICompanyProvider"/> backed by RIK's lihtandmed_v2 SOAP-style
/// service. Credentials live in <see cref="EstoniaOptions"/>; until they are configured
/// (Azure App Config / Key Vault) the adapter returns Unavailable rather than failing.
/// RIK accepts the JSON output format inside the SOAP envelope, which simplifies the
/// payload parser considerably.
/// </summary>
internal sealed class AriregXmlCompanyProvider(
    HttpClient http,
    IOptions<EstoniaOptions> options,
    ILogger<AriregXmlCompanyProvider> logger) : ICompanyProvider
{
    private static readonly XNamespace SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace ProdNs = "http://arireg.x-road.eu/producer/";

    public string CountryCode => "EE";

    public async Task<CompanyLookupResult> LookupAsync(CompanyIdentifier id, CancellationToken ct)
    {
        if (id is not EstonianRegistryCode ee)
        {
            return new CompanyLookupResult.InvalidInput(
                $"AriregXmlCompanyProvider only accepts Estonian registry codes (got {id.CountryCode}:{id.Value}).");
        }

        var opts = options.Value;
        if (!opts.IsConfigured)
        {
            return new CompanyLookupResult.Unavailable(
                "Estonia (RIK) credentials not configured — set Bronnoysund:International:Estonia:Username and :Password in Azure App Config.");
        }

        try
        {
            var soap = BuildLihtandmedRequest(ee.Value, opts.Username, opts.Password);
            using var req = new HttpRequestMessage(HttpMethod.Post, opts.BaseUrl)
            {
                Content = new StringContent(soap, Encoding.UTF8, "text/xml"),
            };
            using var res = await http.SendAsync(req, ct).ConfigureAwait(false);
            if (res.StatusCode == HttpStatusCode.NotFound)
            {
                return new CompanyLookupResult.NotFound(ee.Value);
            }
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var company = ParseLihtandmedResponse(stream);
            return company is null
                ? new CompanyLookupResult.NotFound(ee.Value)
                : new CompanyLookupResult.Found(company);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "RIK transport error for {Code}", ee.Value);
            return new CompanyLookupResult.Unavailable($"Could not contact RIK for {ee.Value}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "RIK timeout for {Code}", ee.Value);
            return new CompanyLookupResult.Unavailable($"RIK did not respond within timeout for {ee.Value}");
        }
    }

    private static string BuildLihtandmedRequest(string registrikood, string username, string password)
    {
        var envelope = new XElement(SoapNs + "Envelope",
            new XAttribute(XNamespace.Xmlns + "SOAP-ENV", SoapNs),
            new XAttribute(XNamespace.Xmlns + "prod", ProdNs),
            new XElement(SoapNs + "Body",
                new XElement(ProdNs + "lihtandmed_v2",
                    new XElement(ProdNs + "keha",
                        new XElement(ProdNs + "ariregister_kasutajanimi", username),
                        new XElement(ProdNs + "ariregister_parool", password),
                        new XElement(ProdNs + "ariregister_valjundi_formaat", "json"),
                        new XElement(ProdNs + "keel", "eng"),
                        new XElement(ProdNs + "ariregistri_kood", registrikood)))));
        return new XDeclaration("1.0", "UTF-8", null) + envelope.ToString(SaveOptions.DisableFormatting);
    }

    private static CompanyResponse? ParseLihtandmedResponse(Stream xml)
    {
        var doc = XDocument.Load(xml);
        // RIK returns JSON inside the SOAP body when ariregister_valjundi_formaat = json.
        var keha = doc.Descendants(ProdNs + "keha").FirstOrDefault();
        if (keha is null) return null;
        var jsonText = keha.Value;
        if (string.IsNullOrWhiteSpace(jsonText)) return null;

        using var json = JsonDocument.Parse(jsonText);
        if (!json.RootElement.TryGetProperty("ettevotjad", out var entities) || entities.GetArrayLength() == 0)
        {
            return null;
        }
        var first = entities[0];
        var code = ReadString(first, "ariregistri_kood");
        var name = ReadString(first, "nimi");
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) return null;

        return new CompanyResponse(
            OrganizationNumber: code!,
            OrganizationName: name!,
            CompanyType: ReadString(first, "evormi_lyhend") ?? "UKJENT",
            LanguageForm: "Unknown",
            Website: ReadString(first, "veebileht"),
            Email: ReadString(first, "elektronposti_aadress"),
            Phone: ReadString(first, "telefon"),
            BusinessAddress: MapAddress(first, "aadress"),
            PostalAddress: null,
            PrimaryIndustry: MapIndustry(first),
            RegisteredDate: ParseDate(ReadString(first, "esmakande_kpv")),
            DeletedDate: ParseDate(ReadString(first, "kustutamise_kpv")),
            IsBankrupt: string.Equals(ReadString(first, "ettevotja_seisund"), "P", StringComparison.OrdinalIgnoreCase));
    }

    private static PostalAddress? MapAddress(JsonElement entity, string property)
    {
        if (!entity.TryGetProperty(property, out var addr) || addr.ValueKind != JsonValueKind.Object) return null;
        return new PostalAddress(
            StreetAddress: ReadString(addr, "ads_adress"),
            PostalCode: ReadString(addr, "ads_postiindeks"),
            City: ReadString(addr, "ehak_nimetus"),
            Municipality: null,
            Country: "EE");
    }

    private static IndustryCode? MapIndustry(JsonElement entity)
    {
        if (!entity.TryGetProperty("teatatud_pohitegevusala", out var act) || act.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        var code = ReadString(act, "emtak_kood");
        var desc = ReadString(act, "emtak_tekstina") ?? string.Empty;
        return string.IsNullOrWhiteSpace(code) ? null : new IndustryCode(code!, desc);
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static DateOnly? ParseDate(string? raw) =>
        DateOnly.TryParse(raw, out var d) ? d : null;
}
