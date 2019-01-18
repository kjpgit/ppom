'use strict';


// Can't believe this isn't a standard thing
// Can't believe str.replace() wont replace all
function escape_html(string) {
    string = String(string);
    return string
        .replace(/&/g, '&amp;')
        .replace(/>/g, '&gt;')
        .replace(/</g, '&lt;')
        .replace(/'/g, '&apos;')
        .replace(/"/g, '&quot;');
}


// To avoid all floating point math errors
// s: a string with exactly two decimal places
// Return: integer (total hundredths)
function parse_hundredths(s) {
    require_string(s);
    if (s.indexOf(".") != (s.length - 3)) {
        throw "Bad decimal format: " + s;
    }
    var s2 = s.replace(".", "");
    var ret = parseInt(s2, 10);
    if (isNaN(ret)) {
        throw "Bad decimal: " + s;
    }
    if (ret < 0) {
        throw "Bad number: " + s;
    }
    return ret;
}


function zero_pad(num, size) {
    require_number(num);
    require_number(size);
    var s = num.toString();
    while (s.length < size) {
        s = "0" + s;
    }
    return s;
}


// The reverse of parse_hundredths()
// Return: string with two decimal places
function hundredths_to_decimal_string(n) {
    require_number(n);
    var ret = Math.floor(n / 100).toString() + "." + zero_pad((n % 100), 2);
    return ret;
}


function precondition(condition, message) {
    if (!condition) {
        throw message || "Precondition failed";
    }
}


function calculate_shipping(total_weight_100, rates) {
    require_number(total_weight_100);
    for (var i = 0; i < rates.length; i++) {
        var rate = rates[i];
        if (total_weight_100 <= parse_hundredths(rate[0])) {
            return parse_hundredths(rate[1]);
        }
    }

    throw "weight exceeds limit";
}


function _get_checked(v) {
    if (v) {
        return 'checked="checked"';
    } else {
        return "";
    }
}


function require_string(obj) {
    if (jQuery.type(obj) !== "string") {
        throw "not a string";
    }
}


function require_number(obj) {
    if (jQuery.type(obj) !== "number") {
        throw "not a number";
    }
}



var myCart = {
    /* change this if we drastically change the schema */
    m_cartName : "patternedpom.mycart",

    STANDARD_RATES: [
        ["0.00",  "0.00"],
        ["0.40",  "3.50"],
        ["0.99",  "5.00"],
        ["2.00",  "10.00"],
        ["3.00",  "12.00"],
        ["4.00",  "20.00"],
        ["10.00", "25.00"],
        ["20.00", "40.00"]
    ],

    PRIORITY_RATES: [
        ["0.00",  "0.00"],
        ["0.40",  "7.00"],
        ["0.99",  "7.00"],
        ["2.00",  "14.00"],
        ["3.00",  "14.00"],
        ["4.00",  "20.00"],
        ["10.00", "25.00"],
        ["20.00", "40.00"]
    ],


    init: function() {
        console.log("myCart init");

        // NB: Add to cart is directly called

        $("button.viewCart").click(function() {
            console.log("viewCart click", this);
            myCart.viewCart();
        });

        // Show cart if we are there
        var obj = $("#cartItems");
        if (obj.length) {
            $(".navcart").hide();
            myCart.drawCart();
        }

        // Empty cart if we are there
        obj = $("#cartEmptyCart");
        if (obj.length) {
            myCart.emptyCart();
        }
    },


    viewCart: function() {
        window.location.href = "/shop/cart.html";
    },


    // Redraw the shopping cart, including the paypal checkout button
    drawCart: function() {
        var cart = this.loadCart();
        var items = cart["cart_items"];
        var shipping_type = cart["cart_shipping_type"];
        var ret = "";
        var totalItemPrice100 = 0;
        var totalWeight100 = 0;
        var totalItemCount = 0;

        // Item Table
        if (!items.length) {
            ret += '<h2>Your cart is empty</h2>';
            ret += '<hr>';
        }

        for (var i = 0; i < items.length; i++) {
            var row = "";
            var item = items[i];
            var rowQuantity = item["itemQuantity"];
            var rowPrice100 = parse_hundredths(item["itemPrice"]) * rowQuantity;
            var rowWeight100 = parse_hundredths(item["itemWeight"]) * rowQuantity;

            row += '<div class="row">';

            // Picture and text description of item
            row += '<div class="col-md-8">';
            row += ''
                + '<a href="' + escape_html(item["itemPath"]) + '">' 
                + '<img class="" src="' + escape_html(item["itemPath"]) + "thumb/thumb.jpg" + '">'
                + '</a>' 
                + '<a class="text-medium" href="' + escape_html(item["itemPath"]) + '">' 
                + escape_html(item["itemName"]) 
                + '</a>' 
                + '<br>';

            row += "<p class='cart-detail text-dim'>Item #" + escape_html(item["itemId"]) + "</p>";
            /*
            row += "<p class='cart-detail'>Weight: " + escape_html(item["itemWeight"]) + "</p>"
            */

            // Show all selected option(s)
            if ("itemOptions" in item) {
                var options = item["itemOptions"];
                for (var j = 0; j < options.length; j++) {
                    var option = options[j];
                    row += "<p class='cart-option cart-detail'>" + escape_html(option[0]) + ": " 
                        + escape_html(option[1]) + "</p>";
                }
            }

            row += '<span class="linkstyle" onclick="myCart.removeCartItem(' + i + ')">Remove</span>' ;
            row += '</div>';

            // Item Price
            row += '<div class="col-xs-6 col-md-2">';
            row += '<span class="text-medium">$' + escape_html(item["itemPrice"]) + "</span>";
            row += '</div>';

            // Item Quantity
            row += '<div class="col-xs-6 col-md-2 align-right">';
            row += ''
                + '<form onsubmit="myCart.updateCart(); return false;">' 
                + '<span class="text-dim">Quantity:&nbsp;</span>'
                + '<input type="number" class=itemquantity required size="4" min="0" max="999"'
                + ' data-item-nr="' + i + '"' 
                + ' oninput="myCart.item_quantity_changed(this)"' 
                + ' value="' + escape_html(item["itemQuantity"]) + '">' 
                + '</input>' 
                + '<br><input class="cart-update" type=submit value="Update Cart">'
                + '</form>';
            row += '</div>';

            row += '</div>'; // row

            row += '<hr>';

            ret += row;

            totalItemPrice100 += rowPrice100;
            totalWeight100 += rowWeight100;
            totalItemCount += rowQuantity;
        }

        console.log("totalItemPrice: " + totalItemPrice100);
        console.log("totalWeight: " + totalWeight100);

        $("#cartItems").html(ret);
        $(".cart-update").hide();

        /* Totals and Checkout */
        /* This can fail if shipping is too large */
        try {
            ret = this.getShippingHTML(items, totalItemCount, totalItemPrice100,
                    totalWeight100, shipping_type);
            $("#cartCheckout").html(ret);
            $('[name="ship"]').change(this.changeShippingRadio);

        } catch (error) {
            console.error(error);
            ret = "<b>Error in shipping calculation</b><br>";
            ret += "<b>You may have too many items</b>";
            $("#cartCheckout").html(ret);
        }
    },

    getShippingHTML: function(items, totalItemCount, totalItemPrice100, 
                             totalWeight100, shipping_type) {
        var ret = "";
        var std_shippingPrice100 = calculate_shipping(totalWeight100, this.STANDARD_RATES);
        var priority_shippingPrice100 = calculate_shipping(totalWeight100, this.PRIORITY_RATES);
        var chosen_shipping100;
        var ischecked;

        if (shipping_type == "Priority") {
            chosen_shipping100 = priority_shippingPrice100;
        } else {
            chosen_shipping100 = std_shippingPrice100;
            shipping_type = "Standard";
        }

        ret += '<p>';
        ret += '<b class=text-large>Subtotal (' + totalItemCount + ' Items): $';
        ret += hundredths_to_decimal_string(totalItemPrice100);
        ret += '</b>';
        ret += '</p>';

        ret += "<form>";
        ret += '<p>';
        ret += 'Choose a USA Shipping Option (does not include order processing time):<br>';

        ischecked = _get_checked(shipping_type === "Standard");
        ret += '<input type=radio id="radio_standard" name="ship" ' + ischecked + 'value="Standard">';
        ret += '<label for="radio_standard">Standard USPS (7 days) ($' + 
                hundredths_to_decimal_string(std_shippingPrice100) + ')</label>';
        ret += '<br>';

        ischecked = _get_checked(shipping_type === "Priority");
        ret += '<input type=radio id="radio_priority" name="ship" ' + ischecked +  'value="Priority">';
        ret += '<label for="radio_priority">Priority USPS (3 days) ($' + 
                hundredths_to_decimal_string(priority_shippingPrice100) + ')</label>';
        ret += '<br>';

        ret += "</p>";
        ret += "</form>";

        ret += '<p>';
        ret += '<b class=text-large>TOTAL: $';
        ret += hundredths_to_decimal_string(totalItemPrice100 + chosen_shipping100);
        ret += '*</b>';
        ret += '<br>';
        ret += '<b>*Sales tax will be added during checkout for Illinois residents</b>';
        ret += '</p>';

        // Checkout button
        if (totalItemCount > 0) {
            ret += this.getPaypalCheckoutForm(items, shipping_type, chosen_shipping100);

            ret += '<p>';
            ret += 'You do not need a PayPal account. ';
            ret += 'Your credit card information is secure and is not sent to this site.';
            ret += '</p>';
        }

        return ret;
    },

    changeShippingRadio: function() {
        console.log("changeShippingRadio");
        var buttons = $('[name="ship"]');
        var chosen = "";
        for (var i = 0; i < buttons.length; i++) {
            if (buttons[i].checked) {
                chosen = buttons[i].value;
            }
        }
        if (chosen !== "") {
            console.log("chosen shipping", chosen);
            myCart.changeShippingPreference(chosen);
            myCart.drawCart();
        }
    },


    getPaypalCheckoutForm: function(items, shipping_type, shipping_total100) {
        var html = "";
        html += '<form action="https://www.sandbox.paypal.com/cgi-bin/webscr" method="post">';
        html += '<p>';
        html += this.getPaypalInput("cmd", "_cart");
        html += this.getPaypalInput("upload", "1");
        html += this.getPaypalInput("business", "karl.pickett-facilitator@gmail.com");
        html += this.getPaypalInput("custom", "test-custom-field");

        /* Return after payment is complete */
        html += this.getPaypalInput("return", "https://d15f32nxt8eigc.cloudfront.net/receipt.html");

        /* This is also shown if they complete checkout and click back */
        html += this.getPaypalInput("cancel_return", "https://d15f32nxt8eigc.cloudfront.net/");

        //html += this.getPaypalInput("display", "1");
        //html += this.getPaypalInput("handling_cart", "1.00");

        var i;
        for (i = 0; i < items.length; i++) {
            var item = items[i];
            var suffix = "_" + (i + 1);
            html += this.getPaypalInput("item_name" + suffix, item["itemName"]);
            html += this.getPaypalInput("item_number" + suffix, item["itemId"]);
            html += this.getPaypalInput("amount" + suffix, item["itemPrice"]);
            html += this.getPaypalInput("quantity" + suffix, item["itemQuantity"]);
            html += this.getPaypalInput("shipping" + suffix, "0.00");

            if ("itemOptions" in item) {
                var options = item["itemOptions"];
                for (var j = 0; j < options.length; j++) {
                    var option = options[j];
                    html += this.getPaypalInput("on" + j + suffix, option[0]);
                    html += this.getPaypalInput("os" + j + suffix, option[1]);
                }
            }
        }

        // shipping is its own "item"
        var suffix = "_" + (i + 1);
        shipping_type = "Shipping Method: " + shipping_type;
        html += this.getPaypalInput("item_name" + suffix, shipping_type);
        html += this.getPaypalInput("shipping" + suffix, hundredths_to_decimal_string(shipping_total100));
        html += this.getPaypalInput("amount" + suffix, "0.00");

        html += '<input type="submit" value="Checkout with PayPal">';
        html += '</p>';
        html += '</form>';
        return html;
    },

    getPaypalInput: function(name, value) {
        var ret = '<INPUT TYPE="hidden" name="' + name + '"'  
            + ' value="' + escape_html(value) + '">';
        return ret;
    },


    // Reparse all quantity fields
    // If any quantity is invalid, error and don't alter cart
    // If quantity is 0 for any item(s), remove it
    updateCart: function() {
        console.log("updateCart");
        var inputs = $('input.itemquantity');
        var cart = this.loadCart();
        var new_items = [];

        // Update quantities from form fields
        for (var i = 0; i < inputs.length; i++) {
            var newQuant = parseInt(inputs[i].value, 10);
            console.log("newQuant", i, newQuant);
            if (isNaN(newQuant) || newQuant < 0 || newQuant > 999) {
                alert("An item has an invalid quantity (please check all rows!).  Quantity must be a number between 0 and 999");
                return false;
            } else if (newQuant > 0) {
                var item = cart["cart_items"][i];
                item["itemQuantity"] = newQuant;
                new_items.push(item);
            } else {
                console.log("removing empty item", i);
            }
        }

        // Everything was ok, change cart
        cart["cart_items"] = new_items;
        this.saveCart(cart);
        this.drawCart();
    },


    // Document the item and cart schema
    getDefaultCart: function() {
        var defaultCart = {
            "cart_shipping_type": "standard",
            "cart_items": [],
            "cart_timestamp": Date.now()
        };
        return defaultCart;
    },


    item_quantity_changed: function(obj) {
        var obj = $(obj);
        var siblings = obj.siblings("input.cart-update");
        $(siblings[0]).show();
        console.log(obj, siblings);
    },


    // Add an item to the cart and save it
    // obj: an item form
    addItem: function(obj) {
        console.log("addCart", this);
        precondition(obj.tagName.toLowerCase() === "form");

        var itemId = $(obj).find('input[name="item-id"]').val();
        var itemName = $(obj).find('input[name="item-name"]').val();
        var itemPath = $(obj).find('input[name="item-path"]').val();
        var itemPrice = $(obj).find('input[name="item-price"]').val();
        var itemWeight = $(obj).find('input[name="item-weight"]').val();
        var itemQuantity = $(obj).find('input[name="item-quantity"]').val();

        require_string(itemId);
        require_string(itemName);
        require_string(itemPath);
        require_string(itemPrice);
        require_string(itemWeight);
        require_string(itemQuantity);  // converted to number below

        itemQuantity = parseInt(itemQuantity, 10);
        if (isNaN(itemQuantity) || itemQuantity < 1 || itemQuantity > 999) {
            alert("Invalid quantity.  Quantity must be a number between 1 and 999");
            return false;
        }
        require_number(itemQuantity);

        var item = {
            "itemId": itemId,
            "itemName": itemName,
            "itemPath": itemPath,
            "itemPrice": itemPrice,
            "itemWeight": itemWeight,
            "itemQuantity": itemQuantity
        };

        // Custom Options
        var itemOptions = [];
        for (var i = 0; i < 10; i++) {
            var optName = $(obj).find('.item-option-name-' + i).val();
            var optValue = $(obj).find('.item-option-value-' + i).val();
            if (optName !== undefined && optValue !== undefined) {
                if (optValue === "") {
                    alert("Please make a selection for all options");
                    return false;
                }
                itemOptions.push([optName, optValue]);
            }
        }

        if (itemOptions.length) {
            console.log(itemOptions);
            item["itemOptions"] = itemOptions;
        }

        myCart.addItemToCart(item);
        myCart.viewCart();
    },

    // Add an item to the cart and save it
    // obj: an item object
    addItemToCart: function(obj) {
        console.log("addItemToCart", JSON.stringify(obj));
        var cart = this.loadCart();
        cart["cart_items"].push(obj);
        this.saveCart(cart);
    },

    changeShippingPreference: function(shipping_type) {
        console.log("saving shipping type: " + shipping_type);
        var cart = this.loadCart();
        cart["cart_shipping_type"] = shipping_type;
        this.saveCart(cart);
    },

    // Empty Cart
    emptyCart: function() {
        console.log("emptying cart");
        localStorage.removeItem(this.m_cartName);
    },

    // Remove an item
    removeCartItem: function(i) {
        var cart = this.loadCart();
        cart["cart_items"].splice(i, 1);
        this.saveCart(cart);
        this.drawCart();
    },


    // Load the current cart from storage
    // Return: the current cart object (may be empty)
    // TODO: don't load carts < 1 week old
    loadCart: function() {
        var data = localStorage.getItem(this.m_cartName);
        var obj;
        if (data != null) {
            obj = JSON.parse(data);
            console.debug("loadCart from storage", obj);
        } else {
            obj = this.getDefaultCart();
            console.debug("loadCart default", obj);
        }
        return obj;
    },


    // Save the current cart to storage
    // obj: a cart object
    saveCart: function(obj) {
        console.debug("saveCart", obj);
        var str = JSON.stringify(obj);
        localStorage.setItem(this.m_cartName, str);
    },

}


console.log('mycart.js loaded');


function _unit_test() {
    var _test_hundredths = function(a, b) {
        precondition(parse_hundredths(a) === b);
        precondition(hundredths_to_decimal_string(b) === a);
    };

    _test_hundredths("1.23", 123);
    _test_hundredths("0.00", 0);
    _test_hundredths("0.01", 1);
    _test_hundredths("0.09", 9);
    _test_hundredths("0.19", 19);
    _test_hundredths("1.09", 109);
    _test_hundredths("1.19", 119);
    _test_hundredths("1.99", 199);
    _test_hundredths("9.09", 909);
    _test_hundredths("10.09", 1009);
    _test_hundredths("1000.02", 100002);
    _test_hundredths("9999.00", 999900);

    precondition(escape_html("''\"\"<<>> abc") === "&apos;&apos;&quot;&quot;&lt;&lt;&gt;&gt; abc");

    precondition(zero_pad(0, 2) === "00");
    precondition(zero_pad(3, 2) === "03");
    precondition(zero_pad(10, 2) === "10");
    precondition(zero_pad(99, 2) === "99");
}

