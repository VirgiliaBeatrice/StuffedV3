/**
 * JSLib module
 */

var moduleJSLib = ESP32.getNativeFunction("ModuleJSLib");
if (moduleJSLib === null) {
	log("Unable to find ModuleJSLib");
	module.exports = null;
	exit;
}

var internalJSLib = {};
moduleJSLib(internalJSLib);

var _core = {
    blockPause: function(ms) {
        internalJSLib.block_pause(ms);
    },

    registerCallback: function(func) {
        internalJSLib.register_callback(func);
    },

    sendCommand: function(arrayBuffer) {
        internalJSLib.send_command(arrayBuffer);
    },

    handleEvent: function() {
        return internalJSLib.handle_event();
    },

    printHeap: function(str) {
        internalJSLib.print_heap(str);
    },

    printStackRemain: function(str) {
        internalJSLib.print_stack_remain(str);
    },


    print: function(str) {
        internalJSLib.print(str);
    },
    printNumber: function(num) {
        internalJSLib.printNumber(num);
    },
    pushEventQueue: function(func) {
        internalJSLib.pushEventQueue(func);
    }
}

var _ret = _core

module.exports = _ret;
