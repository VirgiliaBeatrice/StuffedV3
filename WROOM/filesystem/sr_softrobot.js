var softrobot;
(function (softrobot) {
    var command;
    (function (command) {
        var SetParamType;
        (function (SetParamType) {
            SetParamType[SetParamType["PT_PD"] = 0] = "PT_PD";
            SetParamType[SetParamType["PT_CURRENT"] = 1] = "PT_CURRENT";
            SetParamType[SetParamType["PT_TORQUE_LIMIT"] = 2] = "PT_TORQUE_LIMIT";
            SetParamType[SetParamType["PT_BOARD_ID"] = 3] = "PT_BOARD_ID";
        })(SetParamType = command.SetParamType || (command.SetParamType = {}));
        var CommandIdMovement;
        (function (CommandIdMovement) {
            CommandIdMovement[CommandIdMovement["CI_M_NONE"] = 0] = "CI_M_NONE";
            CommandIdMovement[CommandIdMovement["CI_M_ADD_KEYFRAME"] = 1] = "CI_M_ADD_KEYFRAME";
            CommandIdMovement[CommandIdMovement["CI_M_PAUSE_MOV"] = 2] = "CI_M_PAUSE_MOV";
            CommandIdMovement[CommandIdMovement["CI_M_RESUME_MOV"] = 3] = "CI_M_RESUME_MOV";
            CommandIdMovement[CommandIdMovement["CI_M_PAUSE_INTERPOLATE"] = 4] = "CI_M_PAUSE_INTERPOLATE";
            CommandIdMovement[CommandIdMovement["CI_M_RESUME_INTERPOLATE"] = 5] = "CI_M_RESUME_INTERPOLATE";
            CommandIdMovement[CommandIdMovement["CI_M_CLEAR_MOV"] = 6] = "CI_M_CLEAR_MOV";
            CommandIdMovement[CommandIdMovement["CI_M_CLEAR_PAUSED"] = 7] = "CI_M_CLEAR_PAUSED";
            CommandIdMovement[CommandIdMovement["CI_M_CLEAR_ALL"] = 8] = "CI_M_CLEAR_ALL";
            CommandIdMovement[CommandIdMovement["CI_M_QUERY"] = 9] = "CI_M_QUERY";
            CommandIdMovement[CommandIdMovement["CI_M_COUNT"] = 9] = "CI_M_COUNT";
        })(CommandIdMovement = command.CommandIdMovement || (command.CommandIdMovement = {}));
    })(command = softrobot.command || (softrobot.command = {}));
})(softrobot || (softrobot = {}));
(function (softrobot) {
    var device;
    (function (device) {
        var RobotInfo = (function () {
            function RobotInfo() {
                this.initialize();
            }
            RobotInfo.prototype.initialize = function () {
                this.systemId = 0;
                this.nTarget = 12;
                this.nMotor = 3;
                this.nCurrent = 0;
                this.nForces = 0;
                this.nTouch = 1;
                this.macAddress = new ArrayBuffer(6);
            };
            return RobotInfo;
        }());
        device.RobotInfo = RobotInfo;
        device.robotInfo = new RobotInfo();
        var MotorState = (function () {
            function MotorState() {
                this.initialize();
            }
            MotorState.prototype.initialize = function () {
                this.pose = 0;
                this.velocity = 0;
                this.lengthMin = -5000;
                this.lengthMax = 5000;
                this.controlK = 4096;
                this.controlB = 2048;
                this.controlA = 0;
                this.torqueMin = -1024;
                this.torqueMax = 1024;
            };
            return MotorState;
        }());
        device.MotorState = MotorState;
        var MovementState = (function () {
            function MovementState() {
                this.initialize();
            }
            MovementState.prototype.initialize = function () {
                this.nOccupied = new Array(device.robotInfo.nMotor);
                for (var index = 0; index < this.nOccupied.length; index++) {
                    this.nOccupied[index] = 0;
                }
                this.pausedMovements = [];
            };
            MovementState.prototype.isPaused = function (movementId) {
                for (var i = 0; i < this.pausedMovements.length; i++) {
                    if (this.pausedMovements[i] == movementId)
                        return i;
                }
                return -1;
            };
            MovementState.prototype.pause = function (movementId) {
                if (this.isPaused(movementId) < 0)
                    this.pausedMovements.push(movementId);
            };
            MovementState.prototype.resume = function (movementId) {
                var id = this.isPaused(movementId);
                if (id >= 0)
                    this.pausedMovements.splice(id, 1);
            };
            return MovementState;
        }());
        device.MovementState = MovementState;
        var RobotState = (function () {
            function RobotState() {
                this.initialize();
            }
            RobotState.prototype.initialize = function () {
                this.motor = new Array(device.robotInfo.nMotor);
                for (var index = 0; index < this.motor.length; index++) {
                    this.motor[index] = new MotorState();
                }
                this.current = new Array(device.robotInfo.nCurrent);
                for (var index = 0; index < this.current.length; index++) {
                    this.current[index] = 0;
                }
                this.force = new Array(device.robotInfo.nForces);
                for (var index = 0; index < this.force.length; index++) {
                    this.force[index] = 0;
                }
                this.nInterpolateTotal = 12;
                this.interpolateTargetCountOfWrite = -1;
                this.interpolateTargetCountOfReadMin = 0;
                this.interpolateTargetCountOfReadMax = 0;
                this.interpolateTickMin = 0;
                this.interpolateTickMax = 0;
                this.nInterpolateRemain = 0;
                this.nInterpolateVacancy = 12;
                this.movementState = new MovementState();
            };
            RobotState.prototype.getPropArray = function (name, array) {
                if (!(name in array[0])) {
                    console.log("ERROR: No property named " + name + "in array");
                    return null;
                }
                var res = new Array();
                for (var i = 0; i < array.length; i++) {
                    res.push(array[i][name]);
                }
                return res;
            };
            RobotState.prototype.setPropArray = function (name, pArray, oArray) {
                if (pArray.length != oArray.length) {
                    console.log("Error: Not equivalent length array");
                    return;
                }
                if (!(name in oArray[0])) {
                    console.log("ERROR: No property named " + name + "in array");
                    return;
                }
                var res = oArray;
                for (var index = 0; index < res.length; index++) {
                    res[index][name] = pArray[index];
                }
            };
            return RobotState;
        }());
        device.RobotState = RobotState;
        device.robotState = new RobotState();
        function checkRobotState() {
            function resizeArray(array, size) {
                if (array.length <= size) {
                    var a = new Array(size - array.length);
                    for (var key in a) {
                        if (array.length == 0)
                            a[key] = 0;
                        else
                            a[key] = array[0];
                    }
                    array.concat(a);
                }
                else
                    array.slice(0, size);
                return array;
            }
            function resizeMotorStateArray(array, size) {
                if (array.length <= size) {
                    var a = new Array(size - array.length);
                    for (var key in a) {
                        a[key] = new MotorState();
                    }
                    array.concat(a);
                }
                else
                    array.slice(0, size);
                return array;
            }
            device.robotState.nInterpolateTotal = device.robotInfo.nTarget;
            if (device.robotState.motor.length != device.robotInfo.nMotor)
                device.robotState.motor = resizeMotorStateArray(device.robotState.motor, device.robotInfo.nMotor);
            if (device.robotState.current.length != device.robotInfo.nCurrent)
                device.robotState.current = resizeArray(device.robotState.current, device.robotInfo.nCurrent);
            if (device.robotState.force.length != device.robotInfo.nForces)
                device.robotState.force = resizeArray(device.robotState.force, device.robotInfo.nForces);
            if (device.robotState.movementState.nOccupied.length != device.robotInfo.nMotor)
            device.robotState.movementState.nOccupied = resizeArray(device.robotState.movementState.nOccupied, device.robotInfo.nMotor);
        }
        device.checkRobotState = checkRobotState;
    })(device = softrobot.device || (softrobot.device = {}));
})(softrobot || (softrobot = {}));

// sr_command
(function(softrobot) {
    softrobot.message_command = require("sr_command");
})(softrobot || (softrobot = {}));

(function (softrobot) {
    var message_command;
    (function (message_command) {
        var callbacks;
        (function (callbacks) {
            callbacks.touchQueryer = undefined;
            callbacks.touchQueryerInterval = 500;
        })(callbacks = message_command.callbacks || (message_command.callbacks = {}));
    })(message_command = softrobot.message_command || (softrobot.message_command = {}));
})(softrobot || (softrobot = {}));

(function (softrobot) {
    var message_command;
    (function (message_command) {
        function onReceiveCIBoardinfo(data) {
            softrobot.device.robotInfo = data;
            softrobot.device.checkRobotState();
            for (var i = 0; i < message_command.onRcvCIBoardInfoMessage.length; i++) {
                message_command.onRcvCIBoardInfoMessage[i]();
            }
        }
        message_command.onReceiveCIBoardinfo = onReceiveCIBoardinfo;
        function onReceiveCIDirect(data) {
        }
        message_command.onReceiveCIDirect = onReceiveCIDirect;
        function onReceiveCIUMovement(data) {
            switch (data.movementCommandId) {
                case softrobot.command.CommandIdMovement.CI_M_ADD_KEYFRAME:
                case softrobot.command.CommandIdMovement.CI_M_QUERY:
                    softrobot.device.robotState.movementState.nOccupied = data.nOccupied;
                    break;
                default:
                    break;
            }
            for (var i = 0; i < message_command.onRcvCIUMovementMessage.length; i++) {
                message_command.onRcvCIUMovementMessage[i](data);
            }
        }
        message_command.onReceiveCIUMovement = onReceiveCIUMovement;
        message_command.onRcvCIBoardInfoMessage = [];
        message_command.onRcvCIDirectMessage = [];
        message_command.onRcvCIUMovementMessage = [];
        function setMotorState(to, from) {
            var id = from.motorId;
            if (id >= to.motor.length)
                return;
            if (softrobot.util.haveProp(from.pose))
                to.motor[id].pose = softrobot.util.limitNum(from.pose, to.motor[id].lengthMin, to.motor[id].lengthMax);
            if (softrobot.util.haveProp(from.velocity))
                to.motor[id].velocity = from.velocity;
            if (softrobot.util.haveProp(from.lengthMin))
                to.motor[id].lengthMin = from.lengthMin;
            if (softrobot.util.haveProp(from.lengthMax))
                to.motor[id].lengthMax = from.lengthMax;
            if (softrobot.util.haveProp(from.controlK))
                to.motor[id].controlK = from.controlK;
            if (softrobot.util.haveProp(from.controlB))
                to.motor[id].controlB = from.controlB;
            if (softrobot.util.haveProp(from.controlA))
                to.motor[id].controlA = from.controlA;
            if (softrobot.util.haveProp(from.torqueMin))
                to.motor[id].torqueMin = from.torqueMin;
            if (softrobot.util.haveProp(from.torqueMax))
                to.motor[id].torqueMax = from.torqueMax;
            to.motor[id].pose = softrobot.util.limitNum(to.motor[id].pose, to.motor[id].lengthMin, to.motor[id].lengthMax);
        }
        message_command.setMotorState = setMotorState;
        function updateRemoteMotorState(inst) {
            if (inst.motorId >= softrobot.device.robotInfo.nMotor) {
                console.log("motorId larger than motor number");
                return;
            }
            if (softrobot.util.haveProp(inst.pose) || softrobot.util.haveProp(inst.velocity)) {
                if (softrobot.util.haveProp(inst.pose))
                    softrobot.device.robotState.motor[inst.motorId].pose = softrobot.util.limitNum(inst.pose, softrobot.device.robotState.motor[inst.motorId].lengthMin, softrobot.device.robotState.motor[inst.motorId].lengthMax);
                if (softrobot.util.haveProp(inst.velocity))
                    softrobot.device.robotState.motor[inst.motorId].velocity = inst.velocity;
                var pose = softrobot.device.robotState.getPropArray("pose", softrobot.device.robotState.motor);
                var velocity = softrobot.device.robotState.getPropArray("velocity", softrobot.device.robotState.motor);
                // softrobot.movement.sendKeyframeQueue.clear();
                message_command.setMotorDirect({
                    pose: pose,
                    velocity: velocity
                });
            }
            if (softrobot.util.haveProp(inst.lengthMin) || softrobot.util.haveProp(inst.lengthMax)) {
                if (softrobot.util.haveProp(inst.lengthMin))
                    softrobot.device.robotState.motor[inst.motorId].lengthMin = inst.lengthMin;
                if (softrobot.util.haveProp(inst.lengthMax))
                    softrobot.device.robotState.motor[inst.motorId].lengthMax = inst.lengthMax;
            }
            if (softrobot.util.haveProp(inst.controlK) || softrobot.util.haveProp(inst.controlB)) {
                if (softrobot.util.haveProp(inst.controlK))
                    softrobot.device.robotState.motor[inst.motorId].controlK = inst.controlK;
                if (softrobot.util.haveProp(inst.controlB))
                    softrobot.device.robotState.motor[inst.motorId].controlB = inst.controlB;
                var controlK = softrobot.device.robotState.getPropArray("controlK", softrobot.device.robotState.motor);
                var controlB = softrobot.device.robotState.getPropArray("controlB", softrobot.device.robotState.motor);
                message_command.setMotorParam({
                    paramType: softrobot.command.SetParamType.PT_PD,
                    params1: controlK,
                    params2: controlB
                });
            }
            if (softrobot.util.haveProp(inst.controlA)) {
                if (softrobot.util.haveProp(inst.controlA))
                    softrobot.device.robotState.motor[inst.motorId].controlA = inst.controlA;
                var controlA = softrobot.device.robotState.getPropArray("controlA", softrobot.device.robotState.motor);
                message_command.setMotorParam({
                    paramType: softrobot.command.SetParamType.PT_CURRENT,
                    params1: controlA,
                    params2: undefined
                });
            }
            if (softrobot.util.haveProp(inst.torqueMin) || softrobot.util.haveProp(inst.torqueMax)) {
                if (softrobot.util.haveProp(inst.torqueMin))
                    softrobot.device.robotState.motor[inst.motorId].torqueMin = inst.torqueMin;
                if (softrobot.util.haveProp(inst.torqueMax))
                    softrobot.device.robotState.motor[inst.motorId].torqueMax = inst.torqueMax;
                var torqueMin = softrobot.device.robotState.getPropArray("torqueMin", softrobot.device.robotState.motor);
                var torqueMax = softrobot.device.robotState.getPropArray("torqueMax", softrobot.device.robotState.motor);
                message_command.setMotorParam({
                    paramType: softrobot.command.SetParamType.PT_TORQUE_LIMIT,
                    params1: torqueMin,
                    params2: torqueMax
                });
            }
        }
        message_command.updateRemoteMotorState = updateRemoteMotorState;
        function updateLocalMotorState(inst) {
            if (inst.motorId >= softrobot.device.robotInfo.nMotor) {
                console.log("motorId larger than motor number");
                return;
            }
            setMotorState(softrobot.device.robotState, inst);
        }
        message_command.updateLocalMotorState = updateLocalMotorState;
        function updateRemoteDirect() {
            // softrobot.movement.sendKeyframeQueue.clear();
            message_command.setMotorDirect({
                pose: softrobot.device.robotState.getPropArray("pose", softrobot.device.robotState.motor),
                velocity: softrobot.device.robotState.getPropArray("velocity", softrobot.device.robotState.motor)
            });
        }
        message_command.updateRemoteDirect = updateRemoteDirect;
    })(message_command = softrobot.message_command || (softrobot.message_command = {}));
})(softrobot || (softrobot = {}));

// register callbacks
(function(softrobot) {
    (function (message_command) {
        message_command.registerCallback("onReceiveCIBoardinfo", message_command.onReceiveCIBoardinfo);
        message_command.registerCallback("onReceiveCIDirect", message_command.onReceiveCIDirect);
        message_command.registerCallback("onReceiveCIUMovement", message_command.onReceiveCIUMovement);
    })(message_command = softrobot.message_command || (softrobot.message_command = {}));
})(softrobot || (softrobot = {}));

(function (softrobot) {
    var movement;
    (function (movement) {
        function queryNOccupied() {
            softrobot.message_command.setMovement({
                movementCommandId: softrobot.command.CommandIdMovement.CI_M_QUERY
            });
            jslib.printHeap("---------- queryNOccupied");
        }
        var MovementSender = (function () {
            function MovementSender() {
                this.waitResponse = false;
                softrobot.message_command.onRcvCIUMovementMessage.push(this.onRcvCIUMovementMessage.bind(this));
                this.queryTimer = setInterval(queryNOccupied, MovementSender.OCCUPATION_QUERY_INTERVAL_MS);
            }
            MovementSender.prototype.onRcvCIUMovementMessage = function (data) {
                if (data.movementCommandId == softrobot.command.CommandIdMovement.CI_M_ADD_KEYFRAME || softrobot.command.CommandIdMovement.CI_M_QUERY) {
                    this.waitResponse = false;
                }
            };
            MovementSender.prototype.canAddKeyframe = function (data) {
                if (this.waitResponse)
                    return false;
                for (var i = 0; i < data.motorCount; i++) {
                    if (softrobot.device.robotState.movementState.nOccupied[data.motorId[i]] >= MovementSender.MAX_NOCCUPIED)
                        return false;
                }
                if (softrobot.device.robotState.movementState.isPaused(data.movementId) >= 0)
                    return false;
                return true;
            };
            MovementSender.prototype.send = function (data) {
                switch (data.movementCommandId) {
                    case softrobot.command.CommandIdMovement.CI_M_ADD_KEYFRAME:
                        if (!this.canAddKeyframe(data))
                            return false;
                        this.waitResponse = true;
                        break;
                    case softrobot.command.CommandIdMovement.CI_M_PAUSE_MOV:
                        softrobot.device.robotState.movementState.pause(data.movementId);
                        break;
                    case softrobot.command.CommandIdMovement.CI_M_RESUME_MOV:
                        softrobot.device.robotState.movementState.resume(data.movementId);
                        break;
                    default:
                        break;
                }
                softrobot.message_command.setMovement(data);
                return true;
            };
            MovementSender.MAX_NOCCUPIED = 5;
            MovementSender.OCCUPATION_QUERY_INTERVAL_MS = 3000;
            return MovementSender;
        }());
        movement.MovementSender = MovementSender;
        var lastMovementId = 0;
        function getNewMovementId() {
            lastMovementId = lastMovementId + 1;
            if (lastMovementId > 255)
                lastMovementId = 1;
            return lastMovementId;
        }
        movement.getNewMovementId = getNewMovementId;
    })(movement = softrobot.movement || (softrobot.movement = {}));
})(softrobot || (softrobot = {}));
(function (softrobot) {
    var util;
    (function (util) {
        function haveProp(obj) {
            return !!obj || obj == 0;
        }
        util.haveProp = haveProp;
        function limitNum(num, min, max) {
            var res = num;
            res > max ? (res = max) : (res = res);
            res < min ? (res = min) : (res = res);
            return res;
        }
        util.limitNum = limitNum;
        function interpolate(x1, y1, x2, y2, x) {
            return (y2 - y1) / (x2 - x1) * (x - x1) + y1;
        }
        util.interpolate = interpolate;
    })(util = softrobot.util || (softrobot.util = {}));
})(softrobot || (softrobot = {}));

// module.exports = softrobot;