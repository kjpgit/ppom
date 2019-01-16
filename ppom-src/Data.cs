using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ppom
{
    /// <summary>
    /// A list of options (e.g. for a dropdown).
    /// This class is immutable.
    /// </summary>
    public class OptionList
    {
        public OptionList(String name, IList<String> values)
        {
            Trace.Assert(!String.IsNullOrWhiteSpace(name), "No option name");
            Trace.Assert(values.Any(), "Empty option values");
            foreach (var value in values) {
                Trace.Assert(!String.IsNullOrWhiteSpace(value), "No option value");
            }
            this.name = name;
            this.values = new List<String>(values);
        }

        public String Name => name;
        public IList<String> Values => values.AsReadOnly();

        private String name;
        private List<String> values;
    }


    /// <summary>
    /// Map of String (name) -> OptionList
    /// This class is immutable.
    /// </summary>
    public class OptionDB
    {
        public OptionDB(JArray options)
        {
            this.options = new Dictionary<String, OptionList>();

            foreach (var obj in options) {
                var option = new OptionList(
                    name: (String)obj["title"], 
                    values: obj["values"].ToObject<IList<String>>()
                    );
                // Will throw exception if already exists
                this.options.Add(option.Name, option);
                Debug.WriteLine($"Loaded optionlist {option.Name}");
            }
        }

        public OptionList GetOptionList(String name)
        {
            return options[name];
        }

        private Dictionary<String, OptionList> options;
    }


    /// <summary>
    /// An option to be displayed to the user.
    /// Contains a Label, and optionally a list of choices (OptionList).
    /// If OptionList is null, it is a free-text option.
    /// This class is immutable.
    /// </summary>
    public class ProductOption {
        public ProductOption(String label, OptionList optionList)
        {
            Trace.Assert(!String.IsNullOrWhiteSpace(label));
            this.label = label;
            this.optionList = optionList;
        }

        public String Label => label;
        public OptionList OptionList => optionList;

        private String label;
        private OptionList optionList;
    }


    /// <summary>
    /// Category information.
    /// This class is immutable.
    /// </summary>
    public class Category 
    {
        public Category(String Id, String Name, String Picture) {
            Trace.Assert(!String.IsNullOrWhiteSpace(Id));
            Trace.Assert(!String.IsNullOrWhiteSpace(Name));
            Trace.Assert(!String.IsNullOrWhiteSpace(Picture));

            id = Id;
            name = Name;
            picture = Picture;
        }

        public String Id => id;
        public String Name => name;
        public String Picture => picture;

        private String name;
        private String id;
        private String picture;
    }


    /// <summary>
    /// Map of String (id) -> Category
    /// This class is immutable.
    /// </summary>
    public class CategoryDB {
        public CategoryDB(JArray arr) {
            // Would be nice if .Net had a generic OrderedDictionary :(
            this.categories = new Dictionary<String, Category>();
            this.category_ids = new List<String>();

            foreach (var obj in arr) {
                var category = new Category(
                    Id: (String)obj["id"], 
                    Name: (String)obj["name"], 
                    Picture: (String)obj["picture"]
                    );

                // Will throw exception if already exists
                this.categories.Add(category.Id, category);
                this.category_ids.Add(category.Id);
                Debug.WriteLine($"Loaded category {category.Id}");
            }
        }

        /// <summary>
        /// Return list of category ids
        /// Preserves spreadsheet order (e.g. for front page)
        /// </summary>
        public IList<String> CategoryIds => category_ids.AsReadOnly();

        public Category GetCategory(String id)
        {
            return categories[id];
        }

        private Dictionary<String, Category> categories;
        private List<String> category_ids;  
    }


    /// <summary>
    /// Information for a single product listing.
    /// This class is immutable.
    /// </summary>
    public class Product
    {
        public Product(JObject obj, OptionDB db)
        {
            this.id = (string)obj["id"];
            this.name = (string)obj["name"];
            this.subcategory = (string)obj["subcategory"];
            this.price = Decimal.Parse((string)obj["price"]);
            this.weight = Decimal.Parse((string)obj["weight"]);
            this.options = new List<ProductOption>();

            Trace.Assert(this.price >= 0);
            Trace.Assert(this.weight >= 0);
            Trace.Assert(Extensions.GetDecimalPlaces(this.price) == 2);
            Trace.Assert(Extensions.GetDecimalPlaces(this.weight) == 2);
            Trace.Assert(!String.IsNullOrWhiteSpace(this.id));
            Trace.Assert(!String.IsNullOrWhiteSpace(this.name));
            //Trace.Assert(!String.IsNullOrWhiteSpace(this.subcategory));

            for (var i = 0; i < 10; i++) {
                var label = (string)obj[$"on{i}"];
                if (label != null) {
                    var optionlist_name = (string)obj[$"os{i}"];
                    OptionList ol = null;
                    if (optionlist_name != null) {
                        ol = db.GetOptionList(optionlist_name);
                    }
                    options.Add(new ProductOption(label, ol));
                }
            }
        }

        public String Id => id;
        public String Name => name;
        public String SubCategory => subcategory;
        public Decimal Price => price;
        public Decimal Weight => weight;
        public IList<ProductOption> Options => options.AsReadOnly();

        private String id;
        private String name;
        private String subcategory;
        private Decimal price;
        private Decimal weight;
        private List<ProductOption> options;
    }


    /// <summary>
    /// All store product data (from a spreadsheet).
    /// This class is immutable.
    /// </summary>
    public class StoreData
    {
        public StoreData(String jsonPath)
        {
            JObject root = JObject.Parse(File.ReadAllText(jsonPath));

            this.optionDB = new OptionDB((JArray)root["options"]);
            this.categoryDB = new CategoryDB((JArray)root["categories"]);
            this.products = new List<Product>();

            foreach (var obj in root["products"]) {
                var product = new Product((JObject)obj, optionDB);
                products.Add(product);
                Debug.WriteLine($"Loaded product {product.Id}");
            }
        }

        public OptionDB Options => optionDB;
        public CategoryDB Categories => categoryDB;
        public IList<Product> Products => products.AsReadOnly();

        private OptionDB optionDB;
        private CategoryDB categoryDB;
        private List<Product> products;
    }

}
