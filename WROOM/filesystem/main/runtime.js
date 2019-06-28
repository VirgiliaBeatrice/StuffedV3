var loops = require("sr_loops");
var jslib = require("jslib");
var iot = require("iot");
var console = require("console");
ESP32.include("sr_softrobot.js");
ESP32.include("sr_motor.js");
ESP32.include("sr_callbacks.js");

jslib.printHeap("heap size after require: ");

// init board
softrobot.message_command.requireBoardInfo();

ESP32.include("/main/main.js");

loops.doForever();
