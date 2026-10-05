using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Seclai.Models;

/// <summary>
/// A list response the client fills from either wire shape of a version-gated endpoint.
/// </summary>
internal interface IListPage<TItem>
{
    /// <summary>Stores the items and counters where the model's public properties expose them.</summary>
    void Fill(List<TItem> items, ListShape shape);
}

/// <summary>What the list reader found beside the items.</summary>
internal sealed class ListShape
{
    /// <summary>True when the items came from <c>data</c>, false for a bare array or the endpoint's own key.</summary>
    public bool FromData { get; set; }

    /// <summary>The <c>pagination</c> object, or null when the body has none.</summary>
    public PaginationResponse? Pagination { get; set; }

    /// <summary>The count the body states: inside <c>pagination</c> if it is there, else flat. Null when stated nowhere.</summary>
    public int? Total { get; set; }

    /// <inheritdoc cref="Total"/>
    public int? Page { get; set; }

    /// <inheritdoc cref="Total"/>
    public int? Limit { get; set; }
}

/// <summary>A successful response as received: where it came from and its text, not yet parsed.</summary>
internal readonly struct RawResponse
{
    public RawResponse(Uri url, string? text)
    {
        Url = url;
        Text = text;
    }

    public Uri Url { get; }

    public string? Text { get; }

    /// <summary>The body as JSON. An empty body is an undefined element; text that is not JSON throws <see cref="JsonException"/>.</summary>
    public JsonElement ToJson()
    {
        if (string.IsNullOrWhiteSpace(Text)) return default;
        using var doc = JsonDocument.Parse(Text!);
        return doc.RootElement.Clone();
    }
}
