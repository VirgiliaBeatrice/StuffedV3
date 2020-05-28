var motor;
(function (motor_1) {
    function changeRemoteMotorParameter(motor, parameterType, value) {
        var instruction = {
            motorId: motor
        };
        instruction[parameterType] = value;
        softrobot.message_command.updateRemoteMotorState(instruction);
    }
    motor_1.changeRemoteMotorParameter = changeRemoteMotorParameter;
    function changeLocalStringLength(motor, value) {
        var instruction = {
            motorId: motor,
            pose: value
        };
        softrobot.message_command.updateLocalMotorState(instruction);
    }
    motor_1.changeLocalStringLength = changeLocalStringLength;
    function changeLocalStringVelocity(motor, velocity) {
        var instruction = {
            motorId: motor,
            velocity: velocity
        };
        softrobot.message_command.updateLocalMotorState(instruction);
    }
    motor_1.changeLocalStringVelocity = changeLocalStringVelocity;
    function pushLocalMotorPVToRemoteDirect() {
        softrobot.message_command.updateRemoteDirect();
    }
    motor_1.pushLocalMotorPVToRemoteDirect = pushLocalMotorPVToRemoteDirect;

    var MovementOption;
    (function (MovementOption) {
        MovementOption[MovementOption["play"] = 0] = "play";
        MovementOption[MovementOption["pause"] = 1] = "pause";
        MovementOption[MovementOption["resume"] = 2] = "resume";
        MovementOption[MovementOption["clear"] = 3] = "clear";
    })(MovementOption = motor_1.MovementOption || (motor_1.MovementOption = {}));
    var MovementAllOption;
    (function (MovementAllOption) {
        MovementAllOption[MovementAllOption["pause"] = 0] = "pause";
        MovementAllOption[MovementAllOption["resume"] = 1] = "resume";
        MovementAllOption[MovementAllOption["clear"] = 2] = "clear";
    })(MovementAllOption = motor_1.MovementAllOption || (motor_1.MovementAllOption = {}));
    function getKeyframe(movement, keyframeId) {
        return {
            movementCommandId: softrobot.command.CommandIdMovement.CI_M_ADD_KEYFRAME,
            movementId: movement.movementId,
            keyframeId: keyframeId,
            motorCount: movement.motorIds.length,
            motorId: movement.motorIds,
            period: movement.keyframes[keyframeId].period,
            pose: movement.keyframes[keyframeId].pose,
            refMovementId: 0,
            refKeyframeId: 0,
            refMotorId: 0,
            timeOffset: 0,
            strictMode: true
        };
    }
    motor_1.getKeyframe = getKeyframe;
    function setMovement(option, movement) {
        switch (option) {
            case MovementOption.play: {
                for (var keyframeId = 0; keyframeId < movement.keyframes.length; keyframeId++) {
                    var data = getKeyframe(movement, keyframeId);
                    movementAddKeyframe(data);
                }
                break;
            }
            case MovementOption.pause: {
                pauseMovement(movement);
                break;
            }
            case MovementOption.resume: {
                resumeMovement(movement);
                break;
            }
            case MovementOption.clear: {
                clearMovement(movement);
                break;
            }
        }
    }
    motor_1.setMovement = setMovement;
    function setMovementAll(option) {
        switch (option) {
            case MovementAllOption.pause:
                pauseInterpolate();
                break;
            case MovementAllOption.resume:
                resumeInterpolate();
                break;
            case MovementAllOption.clear:
                clearAllMovements();
                break;
        }
    }
    motor_1.setMovementAll = setMovementAll;
    motor_1.movementNames = ["default_name"];
    function getMovementId(name) {
        for (var i = 0; i < motor_1.movementNames.length; i++) {
            if (motor_1.movementNames[i] == name)
                return i;
        }
        motor_1.movementNames.push(name);
        return motor_1.movementNames.length - 1;
    }
    motor_1.getMovementId = getMovementId;
    function splitToInt(str, seperator) {
        var nums_str = str.split(seperator);
        var nums = [];
        for (var i = 0; i < nums_str.length; i++) {
            if (nums_str[i]==='') continue;
            nums.push(Number(nums_str[i]));
        }
        return nums;
    }
    motor_1.splitToInt = splitToInt;
    function partialKeyframeDecoder(str) {
        var nums = motor_1.splitToInt(str, " ");
        return {
            period: nums[0],
            pose: nums.slice(1)
        };
    }
    motor_1.partialKeyframeDecoder = partialKeyframeDecoder;
    function interpolate(x1, y1, x2, y2, x) {
        return (y2 - y1) / (x2 - x1) * (x - x1) + y1;
    }
    motor_1.interpolate = interpolate;
    function movementDecoder(movementStr) {
        if (movementStr === "")
            return {
                movementId: getMovementId("default_name"),
                motorIds: [0],
                keyframes: [{
                        period: 1000,
                        pose: [0]
                }]
        };
        var lines = movementStr.split("\n");
        var name = lines.splice(0, 1)[0];
        var line_1 = motor_1.splitToInt(lines[0], " ");
        var line_2 = motor_1.splitToInt(lines[1], " ");
        var keyframes_lines = lines.slice(2, 2 + line_1[0]);
        var keyframes = [];
        var time = 0;
        for (var i = 0; i < keyframes_lines.length; i++) {
            keyframes.push(motor_1.partialKeyframeDecoder(keyframes_lines[i]));
            time += keyframes[keyframes.length - 1].period;
        }
        var keyframe_first = keyframes[0], keyframe_last = keyframes[keyframes.length - 1];
        if (time < line_1[2]) {
            var lastPose = [];
            for (var i = 0; i < line_1[1]; i++) {
                lastPose.push(motor_1.interpolate(time - line_1[2], keyframe_last.pose[i], keyframe_first.period, keyframe_first.pose[i], 0));
            }
            keyframes.push({
                period: line_1[2] - time,
                pose: lastPose
            });
        }
        return {
            movementId: getMovementId(name),
            motorIds: line_2.slice(1),
            keyframes: keyframes
        };
    }
    motor_1.movementDecoder = movementDecoder;
    function movementAddKeyframe(data) {
        while (!ESP32.isQuitting() && !softrobot.movement.movementSender.send(data)) {
            loops.pause(200);
        }
    }
    motor_1.movementAddKeyframe = movementAddKeyframe;
    function pauseMovement(movement) {
        softrobot.movement.movementSender.send({
            movementCommandId: softrobot.command.CommandIdMovement.CI_M_PAUSE_MOV,
            movementId: movement.movementId,
            motorCount: movement.motorIds.length,
            motorId: movement.motorIds
        });
    }
    motor_1.pauseMovement = pauseMovement;
    function resumeMovement(movement) {
        softrobot.movement.movementSender.send({
            movementCommandId: softrobot.command.CommandIdMovement.CI_M_RESUME_MOV,
            movementId: movement.movementId,
            motorCount: movement.motorIds.length
        });
    }
    motor_1.resumeMovement = resumeMovement;
    function clearMovement(movement) {
        softrobot.movement.movementSender.send({
            movementCommandId: softrobot.command.CommandIdMovement.CI_M_CLEAR_MOV,
            movementId: movement.movementId,
            motorCount: movement.motorIds.length,
            motorId: movement.motorIds
        });
    }
    motor_1.clearMovement = clearMovement;
    function pauseInterpolate() {
        softrobot.movement.movementSender.send({
            movementCommandId: softrobot.command.CommandIdMovement.CI_M_PAUSE_INTERPOLATE
        });
    }
    motor_1.pauseInterpolate = pauseInterpolate;
    function resumeInterpolate() {
        softrobot.movement.movementSender.send({
            movementCommandId: softrobot.command.CommandIdMovement.CI_M_RESUME_INTERPOLATE
        });
    }
    motor_1.resumeInterpolate = resumeInterpolate;
    function clearAllMovements() {
        softrobot.movement.movementSender.send({
            movementCommandId: softrobot.command.CommandIdMovement.CI_M_CLEAR_ALL
        });
    }
    motor_1.clearAllMovements = clearAllMovements;
    function clearPausedMovements() {
        softrobot.movement.movementSender.send({
            movementCommandId: softrobot.command.CommandIdMovement.CI_M_CLEAR_PAUSED
        });
    }
    motor_1.clearPausedMovements = clearPausedMovements;
    (function (MovementState) {
        MovementState[MovementState["playing"] = 0] = "playing";
        MovementState[MovementState["paused"] = 1] = "paused";
        MovementState[MovementState["finished"] = 2] = "finished";
    })(MovementState = motor_1.MovementState || (motor_1.MovementState = {}));
    motor_1.isMovementState = softrobot.movement.movementSender.isMovementState;
    (function (TimeRelationship) {
        TimeRelationship[TimeRelationship["with"] = 0] = "with";
        TimeRelationship[TimeRelationship["after"] = 1] = "after";
    })(TimeRelationship = motor_1.TimeRelationship || (motor_1.TimeRelationship = {}));
    function playRelativeToTime(movement, refMovement, relationship, offset) {
        switch (relationship) {
            case TimeRelationship.with: {
                var timeOffset = offset;
                var refKeyframeId = 0;
                for (var keyframeId = 0; keyframeId < movement.keyframes.length; keyframeId++) {
                    var data = getKeyframe(movement, keyframeId);

                    data.refMovementId = refMovement.movementId;
                    data.refMotorId = refMovement.motorIds[0];

                    while ((refKeyframeId < refMovement.keyframes.length - 1) && (timeOffset - refMovement.keyframes[refKeyframeId + 1].period >= 0)) {
                        timeOffset -= refMovement.keyframes[refKeyframeId].period;
                        refKeyframeId += 1;
                    }
                    data.refKeyframeId = refKeyframeId;
                    data.timeOffset = timeOffset;

                    movementAddKeyframe(data);

                    timeOffset += data.period;
                }
                break;
            }
            case TimeRelationship.after: {
                var timeOffset = offset;
                for (var keyframeId = 0; keyframeId < movement.keyframes.length; keyframeId++) {
                    var data = getKeyframe(movement, keyframeId);

                    data.refKeyframeId = refMovement.movementId;
                    data.refMotorId = refMovement.motorIds[0];
                    data.timeOffset = timeOffset;
                    data.strictMode = false;

                    movementAddKeyframe(data);

                    timeOffset += data.period;
                }
                break;
            }
        }
    }
    motor_1.playRelativeToTime = playRelativeToTime;
    function getDuration(movement) {
        var duration = 0
        for (var i = 0; i < movement.keyframes.length; i++) {
            duration += movement.keyframes[i].period
        }
        return duration
    }
    motor_1.getDuration = getDuration;
    function remainingCount(movement) {
        return softrobot.movement.movementSender.getMovementCount(movement.movementId);
    }
    motor_1.remainingCount = remainingCount;
})(motor || (motor = {}));
