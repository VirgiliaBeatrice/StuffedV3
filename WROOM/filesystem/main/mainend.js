// register callback
// jslib.registerCallback(function(data){
//     let p = softrobot.packet_command.Packet.fromBinary(data.slice(2));
//     if (!p) {
//         console.log("Jsfile Callback: Unable to convert Arraybuffer to Packet");
//         return;
//     }
//     softrobot.message_command.messageHandler(p);
//     console.log("Jsfile Callback: callback called");
// });

// start loop
loops.doForever();