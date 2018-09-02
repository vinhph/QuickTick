
using EPiServer.Find;
using System;
using System.Collections.Generic;
using System.Linq;
namespace OSG.Search
{
    public class FindDocument
    {
        /// <summary>The numeric catalog entry identifier (unique).</summary>
        [Id]
        public int CatalogEntryId { get; set; }

        /// <summary>
        /// The string catalog entry identifier (unique per application id).
        /// </summary>
        public string CatalogEntryCode { get; set; }

        /// <summary>The name of the metaclass for this entry.</summary>
        public string MetaClassName { get; set; }

        /// <summary>
        /// The type of the catalog entry (variation, product, etc).
        /// </summary>
        public string CatalogEntryType { get; set; }

        /// <summary>The flag define this entry allow preorder or not.</summary>
        public bool AllowPreorder { get; set; }

        /// <summary>The preorder available date of the entry in UTC.</summary>
        public DateTime PreorderAvailableDate { get; set; }

        /// <summary>The start date of the entry in UTC.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>The end date of the entry in UTC.</summary>
        public DateTime EndDate { get; set; }

        /// <summary>The available languages for the entry.</summary>
        public List<string> Languages { get; set; }

        /// <summary>The available currencies for the entry.</summary>
        public List<string> Currencies { get; set; }

        /// <summary>The available markets for the entry.</summary>
        public List<string> Markets { get; set; }

        /// <summary>The catalogs which contain the entry.</summary>
        public List<string> Catalogs { get; set; }

        /// <summary>The outlines describing the entry's locations.</summary>
        /// <remarks>
        /// The outline is formatted as {catalog name}/{node code}/.../{node code}.
        /// Any forward slash in a catalog name or node code will be converted to an underscore, and all outlines are indexed as lower case.
        /// </remarks>
        public List<string> Outlines { get; set; }

        /// <summary>All individual node codes found in the outlines.</summary>
        public List<string> CatalogNodes { get; set; }

        /// <summary>The name of the entry.</summary>
        public string Name { get; set; }

        /// <summary>Flag indicates entry is active or not.</summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The lowest price available to all customers at any quantity, for each configured currency and market.
        /// </summary>
        public Dictionary<string, double?> ListPrices { get; set; }

        /// <summary>
        /// The lowest price for each configured currency and market.
        /// </summary>
        /// <remarks>
        /// The lowest price is taken from all prices, regardless of customer pricing or minimum quantity.
        /// </remarks>
        public Dictionary<string, double?> SalePrices { get; set; }

        /// <summary>All metafields that are indexed as string.</summary>
        public Dictionary<string, string> StringFields { get; set; }

        /// <summary>
        /// All metafields that are indexed as collections of strings.
        /// </summary>
        public Dictionary<string, List<string>> StringCollectionFields { get; set; }

        /// <summary>All metafields that are indexed as integers.</summary>
        public Dictionary<string, int?> IntFields { get; set; }

        /// <summary>All metafields that are indexed as booleans.</summary>
        public Dictionary<string, bool?> BoolFields { get; set; }

        /// <summary>All metafields that are indexed as longs.</summary>
        public Dictionary<string, long?> LongFields { get; set; }

        /// <summary>All metafield that are indexed as doubles.</summary>
        public Dictionary<string, double?> DoubleFields { get; set; }

        /// <summary>All metafields that are indexed as datetimes.</summary>
        public Dictionary<string, DateTime?> DateTimeFields { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:EPiServer.Commerce.FindSearchProvider.FindDocument" /> class.
        /// </summary>
        public FindDocument()
        {
            this.Languages = new List<string>();
            this.Currencies = new List<string>();
            this.Markets = new List<string>();
            this.Catalogs = new List<string>();
            this.Outlines = new List<string>();
            this.CatalogNodes = new List<string>();
            this.ListPrices = new Dictionary<string, double?>();
            this.SalePrices = new Dictionary<string, double?>();
            this.StringFields = new Dictionary<string, string>();
            this.StringCollectionFields = new Dictionary<string, List<string>>();
            this.IntFields = new Dictionary<string, int?>();
            this.BoolFields = new Dictionary<string, bool?>();
            this.LongFields = new Dictionary<string, long?>();
            this.DoubleFields = new Dictionary<string, double?>();
            this.DateTimeFields = new Dictionary<string, DateTime?>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:EPiServer.Commerce.FindSearchProvider.FindDocument" /> class.
        /// </summary>
        /// <param name="source">The source.</param>
        public FindDocument(FindDocument source)
        {
            this.CatalogEntryId = source.CatalogEntryId;
            this.CatalogEntryCode = source.CatalogEntryCode;
            this.CatalogEntryType = source.CatalogEntryType;
            this.AllowPreorder = source.AllowPreorder;
            this.PreorderAvailableDate = source.PreorderAvailableDate;
            this.StartDate = source.StartDate;
            this.EndDate = source.EndDate;
            this.Languages = source.Languages.ToList<string>();
            this.Currencies = source.Currencies.ToList<string>();
            this.Markets = source.Markets.ToList<string>();
            this.Catalogs = source.Catalogs.ToList<string>();
            this.Outlines = source.Outlines.ToList<string>();
            this.CatalogNodes = source.CatalogNodes.ToList<string>();
            this.Name = source.Name;
            this.IsActive = source.IsActive;
            this.ListPrices = source.ListPrices.ToDictionary<KeyValuePair<string, double?>, string, double?>((Func<KeyValuePair<string, double?>, string>)(kv => kv.Key), (Func<KeyValuePair<string, double?>, double?>)(kv => kv.Value));
            this.SalePrices = source.SalePrices.ToDictionary<KeyValuePair<string, double?>, string, double?>((Func<KeyValuePair<string, double?>, string>)(kv => kv.Key), (Func<KeyValuePair<string, double?>, double?>)(kv => kv.Value));
            this.StringFields = source.StringFields.ToDictionary<KeyValuePair<string, string>, string, string>((Func<KeyValuePair<string, string>, string>)(kv => kv.Key), (Func<KeyValuePair<string, string>, string>)(kv => kv.Value));
            this.StringCollectionFields = source.StringCollectionFields.ToDictionary<KeyValuePair<string, List<string>>, string, List<string>>((Func<KeyValuePair<string, List<string>>, string>)(kv => kv.Key), (Func<KeyValuePair<string, List<string>>, List<string>>)(kv => kv.Value.ToList<string>()));
            this.IntFields = source.IntFields.ToDictionary<KeyValuePair<string, int?>, string, int?>((Func<KeyValuePair<string, int?>, string>)(kv => kv.Key), (Func<KeyValuePair<string, int?>, int?>)(kv => kv.Value));
            this.BoolFields = source.BoolFields.ToDictionary<KeyValuePair<string, bool?>, string, bool?>((Func<KeyValuePair<string, bool?>, string>)(kv => kv.Key), (Func<KeyValuePair<string, bool?>, bool?>)(kv => kv.Value));
            this.LongFields = source.LongFields.ToDictionary<KeyValuePair<string, long?>, string, long?>((Func<KeyValuePair<string, long?>, string>)(kv => kv.Key), (Func<KeyValuePair<string, long?>, long?>)(kv => kv.Value));
            this.DoubleFields = source.DoubleFields.ToDictionary<KeyValuePair<string, double?>, string, double?>((Func<KeyValuePair<string, double?>, string>)(kv => kv.Key), (Func<KeyValuePair<string, double?>, double?>)(kv => kv.Value));
            this.DateTimeFields = source.DateTimeFields.ToDictionary<KeyValuePair<string, DateTime?>, string, DateTime?>((Func<KeyValuePair<string, DateTime?>, string>)(kv => kv.Key), (Func<KeyValuePair<string, DateTime?>, DateTime?>)(kv => kv.Value));
        }
    }
}
