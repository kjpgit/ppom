using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
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
            this.Name = name;
            this.values = new List<String>(values);
        }

        public String Name { get; }
        public IList<String> Values => values.AsReadOnly();

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
            this.Label = label;
            this.OptionList = optionList;
        }

        public String Label { get; }
        public OptionList OptionList { get; }
    }


    /// <summary>
    /// Category information.
    /// This class is immutable.
    /// </summary>
    public class Category 
    {
        public Category(String id, String name, String description, String picture) {
            Trace.Assert(!String.IsNullOrWhiteSpace(id));
            Trace.Assert(!String.IsNullOrWhiteSpace(name));
            Trace.Assert(!String.IsNullOrWhiteSpace(picture));

            Id = id;
            Name = name;
            Picture = picture;
            Description = description;
        }

        public String Id { get; }
        public String Name { get; }
        public String Picture { get; }
        public String Description { get; }  // may be null
    }


    /// <summary>
    /// Information for a single product listing.
    /// This class is immutable.
    /// </summary>
    public class Product
    {
        public Product(String id, JObject obj, OptionDB db, Category category,
                FileData fileData, String sourceDir)
        {
            this.Id = id;
            this.Name = (string)obj["name"];
            this.SubCategory = (string)obj["subcategory"];
            this.Price = Decimal.Parse((string)obj["price"]);
            this.Weight = Decimal.Parse((string)obj["weight"]);
            this.options = new List<ProductOption>();
            this.extraImages = new List<String>();
            this.Category = category;
            this.Description = fileData.GetProductDescriptionHTML(sourceDir);
            this.fileData = fileData;
            this.sourceDir = sourceDir;

            Trace.Assert(this.Price >= 0);
            Trace.Assert(this.Weight >= 0);
            Trace.Assert(Extensions.GetDecimalPlaces(this.Price) == 2);
            Trace.Assert(Extensions.GetDecimalPlaces(this.Weight) == 2);
            Trace.Assert(!String.IsNullOrWhiteSpace(this.Id));
            Trace.Assert(!String.IsNullOrWhiteSpace(this.Name));
            Trace.Assert(!String.IsNullOrWhiteSpace(this.Description));
            Trace.Assert(this.Category != null);
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

            string images = (string)obj["extra_images"];
            if (images != null) {
                var split_images = images.Split(null);
                this.extraImages.AddRange(split_images);
            }
        }

        public String Id { get; }
        public String Name { get; }
        public Category Category { get; }
        public String SubCategory { get; }
        public String Description { get; }
        public Decimal Price { get; }
        public Decimal Weight { get; }
        public IList<ProductOption> Options => options.AsReadOnly();
        public IList<String> ExtraImages => extraImages.AsReadOnly();

        public IList<String> ImageNames {
            get {
                return (from path in fileData.GetImagePaths(this)
                        select Path.GetFileName(path)).ToList();
            }
        } 

        private List<ProductOption> options;
        private List<String> extraImages;
        private String sourceDir;
        private FileData fileData;
    }


    /// <summary>
    /// All store product data (from a spreadsheet).
    /// This class is immutable.
    /// </summary>
    public class StoreData
    {
        public StoreData(String jsonPath, FileData fileData)
        {
            JObject root = JObject.Parse(File.ReadAllText(jsonPath));

            this.Options = new OptionDB((JArray)root["options"]);
            this.categories = new OrderedDictionary();
            this.products = new List<Product>();
            this.fileData = fileData;

            foreach (var obj in root["categories"]) {
                var category = new Category(
                    id: (String)obj["id"], 
                    name: (String)obj["name"], 
                    picture: (String)obj["picture"],
                    description: fileData.GetCategoryDescriptionHTML((String)obj["id"])
                    );

                // Will throw exception if already exists
                this.categories.Add(category.Id, category);
                Debug.WriteLine($"Loaded category {category.Id}");
            }

            // category_id is not in the spreadsheet.  Figure it out from the filesystem.
            var productToCategoryMap = new Dictionary<String, String>();
            foreach (var category in this.Categories) {
                foreach (var productId in fileData.GetProductIdsForCategory(category.Id)) {
                    Console.WriteLine($"product {productId} -> {category.Id}");
                    productToCategoryMap[productId] = category.Id;
                }
            }

            foreach (var obj in root["products"]) {
                var productId = (string)obj["id"];
                if (!productToCategoryMap.ContainsKey(productId)) {
                    Console.WriteLine($"Warning: {productId} not in folder");
                } else {
                    var category = GetCategoryById(productToCategoryMap[productId]);
                    var productDir = fileData.GetProductDirectory(category.Id, productId);
                    var description = fileData.GetProductDescriptionHTML(productDir);
                    var product = new Product(productId, (JObject)obj, Options,
                        category, fileData, productDir);
                    products.Add(product);
                    Debug.WriteLine($"Loaded product {product.Id}");
                }
            }
        }

        public OptionDB Options { get; }

        public Category GetCategoryById(string categoryId) {
            return (Category)categories[categoryId];
        }

        public List<Category> Categories {
            get {
                return (from category in categories.Values.Cast<Category>()
                        select category).ToList();
            }
        }

        public IList<Product> Products => products.AsReadOnly();

        private OrderedDictionary categories;
        private List<Product> products;
        private FileData fileData;
    }

}
