using System;
using System.Linq.Expressions;
using EPiServer.Commerce.FindSearchProvider;
using Mediachase.Commerce;

namespace OSG.Search
{
    internal abstract class ProviderFieldConfiguration : IFieldConfiguration
    {
        public bool IsIncludedInDefaultSearch { get; protected set; }

        public string Name { get; protected set; }

        public virtual string Locale { get; protected set; }

        public virtual Mediachase.Commerce.Currency? Currency { get; protected set; }

        public virtual Mediachase.Commerce.MarketId? MarketId { get; protected set; }

        public bool IsDynamicField { get; protected set; }

        public abstract Type Type { get; }

        public abstract LambdaExpression GetValueExpression { get; }

        public abstract ProviderFieldConfiguration CloneRenamed(string newName);

        public abstract object GetObjectValue(FindDocument document);

        public abstract void SetObjectValue(FindDocument document, object value);
    }
    internal class ProviderFieldConfiguration<TField> : ProviderFieldConfiguration, IFieldConfiguration<TField>, IFieldConfiguration
    {
        private Func<FindDocument, TField> _safeTypedGetValueExpression;
        private Action<FindDocument, TField> _setFieldAction;

        public Expression<Func<FindDocument, object>> FieldExpression { get; private set; }

        public Expression<Func<FindDocument, TField>> TypedGetValueExpression { get; private set; }

        public override Type Type
        {
            get
            {
                return typeof(TField);
            }
        }

        public override LambdaExpression GetValueExpression
        {
            get
            {
                return (LambdaExpression)this.TypedGetValueExpression;
            }
        }

        private ProviderFieldConfiguration()
        {
        }

        public ProviderFieldConfiguration(string name, string locale, Mediachase.Commerce.Currency? currency, MarketId? marketId, bool isDynamicField, bool isIncludedInDefaultSearch, Expression<Func<FindDocument, TField>> getter, Func<FindDocument, TField> safeGetter, Action<FindDocument, TField> setter)
        {
            if (locale != null && currency.HasValue)
                throw new Exception("Locale and currency can not both be non-null.");
            this.Name = name;
            this.Locale = locale;
            this.Currency = currency;
            this.MarketId = marketId;
            this.IsDynamicField = isDynamicField;
            this.IsIncludedInDefaultSearch = isIncludedInDefaultSearch;
            this.TypedGetValueExpression = getter;
            this._safeTypedGetValueExpression = safeGetter;
            this._setFieldAction = setter;
        }

        public ProviderFieldConfiguration(string name, Expression<Func<FindDocument, TField>> getter)
          : this(name, (string)null, new Mediachase.Commerce.Currency?(), new MarketId?(), false, false, getter, getter.Compile(), (Action<FindDocument, TField>)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:EPiServer.Commerce.FindSearchProvider.LocalImplementation.ProviderFieldConfiguration`1" /> class, with option to include in default search.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        /// <param name="getter">The getter expression.</param>
        /// <param name="isIncludedInDefaultSearch">if set to <c>true</c> this field will be included in default search.</param>
        public ProviderFieldConfiguration(string name, Expression<Func<FindDocument, TField>> getter, bool isIncludedInDefaultSearch)
          : this(name, (string)null, new Mediachase.Commerce.Currency?(), new MarketId?(), false, isIncludedInDefaultSearch, getter, getter.Compile(), (Action<FindDocument, TField>)null)
        {
        }

        public override ProviderFieldConfiguration CloneRenamed(string newName)
        {
            ProviderFieldConfiguration<TField> fieldConfiguration = new ProviderFieldConfiguration<TField>();
            fieldConfiguration.Name = newName;
            fieldConfiguration.Locale = (string)null;
            fieldConfiguration.Currency = new Mediachase.Commerce.Currency?();
            fieldConfiguration.MarketId = new MarketId?();
            fieldConfiguration.IsDynamicField = this.IsDynamicField;
            fieldConfiguration.IsIncludedInDefaultSearch = this.IsIncludedInDefaultSearch;
            fieldConfiguration.FieldExpression = this.FieldExpression;
            fieldConfiguration.TypedGetValueExpression = this.TypedGetValueExpression;
            fieldConfiguration._safeTypedGetValueExpression = this._safeTypedGetValueExpression;
            fieldConfiguration._setFieldAction = this._setFieldAction;
            return (ProviderFieldConfiguration)fieldConfiguration;
        }

        public override object GetObjectValue(FindDocument document)
        {
            return (object)this._safeTypedGetValueExpression(document);
        }

        public TField GetValue(FindDocument document)
        {
            return this._safeTypedGetValueExpression(document);
        }

        public void SetValue(FindDocument document, TField value)
        {
            this._setFieldAction(document, value);
        }

        public override void SetObjectValue(FindDocument document, object value)
        {
            this.SetValue(document, (TField)value);
        }

        public override string ToString()
        {
            return this.FieldExpression.ToString();
        }
    }
}
