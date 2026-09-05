using System.Globalization;
using System.Text;

namespace ReceiptReader.Models;

/// <summary>
/// Parameters of a Montenegro tax InvoiceCheck (<c>mapr.tax.gov.me/ic</c>) link.
/// </summary>
internal sealed class TaxReceiptLink
{
    /// <summary>
    /// URL key <c>iic</c>. Receipt labels: IIC, IKOF (Issuer Identification Code).
    /// </summary>
    public required string InvoiceIdentificationCode { get; init; }

    /// <summary>
    /// URL key <c>tin</c>. Receipt labels: TIN, PIB (poreski identifikacioni broj / poreski broj izdavaoca).
    /// </summary>
    public required string TaxpayerIdentificationNumber { get; init; }

    /// <summary>
    /// URL key <c>crtd</c>. Receipt labels: Date and time of issue; datum i vrijeme kreiranja.
    /// </summary>
    public required DateTimeOffset Created { get; init; }

    /// <summary>
    /// URL key <c>prc</c>. Receipt labels: Total price; ukupna cijena.
    /// </summary>
    public required decimal TotalPrice { get; init; }

    /// <summary>
    /// URL key <c>ord</c>. Receipt labels: Invoice number; broj naloga za račun.
    /// </summary>
    public int? OrdinalNumber { get; init; }

    /// <summary>
    /// URL key <c>bu</c>. Receipt labels: Business unit code; poslovna jedinica.
    /// </summary>
    public string? BusinessUnitCode { get; init; }

    /// <summary>
    /// URL key <c>cr</c>. Receipt labels: Cash register code; ENU (elektronski naplatni uređaj).
    /// </summary>
    public string? CashRegisterCode { get; init; }

    /// <summary>
    /// URL key <c>sw</c>. Receipt labels: Software code; kod softvera.
    /// </summary>
    public string? SoftwareCode { get; init; }

    /// <summary>
    /// Composes the InvoiceCheck URL for these parameters.
    /// </summary>
    public string ComposeUrl()
    {
        var sb = new StringBuilder();
        sb.Append(InvoiceCheckUrlParts.ComposeBase);

        AppendQueryParam(sb, "iic", InvoiceIdentificationCode);
        AppendQueryParam(sb, "tin", TaxpayerIdentificationNumber);
        AppendQueryParam(
            sb,
            "crtd",
            Created.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture),
            encode: false);

        if (OrdinalNumber is int ordinal)
        {
            AppendQueryParam(sb, "ord", ordinal.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(BusinessUnitCode))
        {
            AppendQueryParam(sb, "bu", BusinessUnitCode);
        }

        if (!string.IsNullOrWhiteSpace(CashRegisterCode))
        {
            AppendQueryParam(sb, "cr", CashRegisterCode);
        }

        if (!string.IsNullOrWhiteSpace(SoftwareCode))
        {
            AppendQueryParam(sb, "sw", SoftwareCode);
        }

        AppendQueryParam(
            sb,
            "prc",
            TotalPrice.ToString("0.00", CultureInfo.InvariantCulture));

        return sb.ToString();
    }

    /// <summary>
    /// Parses an InvoiceCheck URL into link parameters.
    /// Uses the same host/path/fragment shell rules as <c>UrlValidator</c>.
    /// </summary>
    public static bool TryParse(string? url, out TaxReceiptLink link)
    {
        link = null!;

        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        if (!string.Equals(uri.Host, InvoiceCheckUrlParts.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!uri.AbsolutePath.Equals(InvoiceCheckUrlParts.AbsolutePath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!uri.Fragment.StartsWith(InvoiceCheckUrlParts.FragmentIicPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!TryParseFragmentQuery(uri.Fragment, out var values))
        {
            return false;
        }

        if (!values.TryGetValue("iic", out var iic) || string.IsNullOrWhiteSpace(iic))
        {
            return false;
        }

        if (!values.TryGetValue("tin", out var tin) || string.IsNullOrWhiteSpace(tin))
        {
            return false;
        }

        if (!values.TryGetValue("crtd", out var crtdRaw)
            || !TryParseCreated(crtdRaw, out var created))
        {
            return false;
        }

        if (!values.TryGetValue("prc", out var prcRaw)
            || !decimal.TryParse(
                prcRaw,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var totalPrice)
            || totalPrice < 0m)
        {
            return false;
        }

        int? ordinal = null;
        if (values.TryGetValue("ord", out var ordRaw))
        {
            if (!int.TryParse(ordRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ord))
            {
                return false;
            }

            ordinal = ord;
        }

        string? businessUnit = null;
        if (values.TryGetValue("bu", out var buRaw))
        {
            if (string.IsNullOrWhiteSpace(buRaw))
            {
                return false;
            }

            businessUnit = buRaw;
        }

        string? cashRegister = null;
        if (values.TryGetValue("cr", out var crRaw))
        {
            if (string.IsNullOrWhiteSpace(crRaw))
            {
                return false;
            }

            cashRegister = crRaw;
        }

        string? software = null;
        if (values.TryGetValue("sw", out var swRaw))
        {
            if (string.IsNullOrWhiteSpace(swRaw))
            {
                return false;
            }

            software = swRaw;
        }

        link = new TaxReceiptLink
        {
            InvoiceIdentificationCode = iic,
            TaxpayerIdentificationNumber = tin,
            Created = created,
            TotalPrice = totalPrice,
            OrdinalNumber = ordinal,
            BusinessUnitCode = businessUnit,
            CashRegisterCode = cashRegister,
            SoftwareCode = software
        };

        return true;
    }

    /// <summary>
    /// Copies link identity/metadata onto <paramref name="invoice"/>.
    /// Does not set <see cref="Invoice.URL"/>, <see cref="Invoice.TotalSum"/>,
    /// <see cref="Invoice.ShopName"/>, <see cref="Invoice.ShoppingDate"/>, or <see cref="Invoice.CreatedAt"/>.
    /// </summary>
    public void ApplyTo(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        invoice.InvoiceIdentificationCode = InvoiceIdentificationCode;
        invoice.TaxpayerIdentificationNumber = TaxpayerIdentificationNumber;
        invoice.Created = Created;
        invoice.OrdinalNumber = OrdinalNumber;
        invoice.BusinessUnitCode = BusinessUnitCode;
        invoice.CashRegisterCode = CashRegisterCode;
        invoice.SoftwareCode = SoftwareCode;
    }

    private static bool TryParseFragmentQuery(
        string fragment,
        out Dictionary<string, string> values)
    {
        values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!fragment.StartsWith(InvoiceCheckUrlParts.FragmentPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var query = fragment[InvoiceCheckUrlParts.FragmentPrefix.Length..];
        if (query.Length == 0)
        {
            return false;
        }

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = part.IndexOf('=');
            if (separatorIndex <= 0)
            {
                return false;
            }

            var key = Uri.UnescapeDataString(part[..separatorIndex]);
            var value = Uri.UnescapeDataString(part[(separatorIndex + 1)..]);

            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            // First occurrence wins; duplicate keys are ignored.
            values.TryAdd(key, value);
        }

        return values.Count > 0;
    }

    private static bool TryParseCreated(string raw, out DateTimeOffset created)
    {
        if (DateTimeOffset.TryParseExact(
                raw,
                "yyyy-MM-dd'T'HH:mm:sszzz",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out created))
        {
            return true;
        }

        return DateTimeOffset.TryParse(
            raw,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out created);
    }

    private static void AppendQueryParam(
        StringBuilder sb,
        string key,
        string value,
        bool encode = true)
    {
        if (sb[^1] != '?')
        {
            sb.Append('&');
        }

        sb.Append(key);
        sb.Append('=');
        sb.Append(encode ? Uri.EscapeDataString(value) : value);
    }
}
