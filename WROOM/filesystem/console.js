var jslib = require("jslib");

var _ret = {
    log: function (str) {
        jslib.print(str);
    },
    logNumber: function(num) {
        jslib.printNumber(num);
    }
}

module.exports = _ret
