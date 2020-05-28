#include "module_device.h"

JSRobotState jsRobotState(allBoards);

JSRobotState::JSRobotState(AllBoards& allBoards)  {
    struct JSMotorState template_motor_state = {
        0, 0, -5000, 5000, 4096, 2048, 0, -1024, 1024
    };
    motor.resize(allBoards.GetNTotalMotor(), template_motor_state);
    current.resize(allBoards.GetNTotalCurrent(), 0);
    force.resize(allBoards.GetNTotalForce(), 0);
    touch.resize(allBoards.GetNTotalTouch(), -1);

    readSemaphore = xSemaphoreCreateMutex();
    writeSemaphore = xSemaphoreCreateMutex();
}

JSRobotState::~JSRobotState() {
    vSemaphoreDelete(readSemaphore);
    vSemaphoreDelete(writeSemaphore);
}

void JSRobotState::read_lock() {
    //xSemaphoreTake(writeSemaphore, portMAX_DELAY);
}
void JSRobotState::read_unlock() {
    //xSemaphoreGive(writeSemaphore);
}
void JSRobotState::write_lock() {
    //xSemaphoreTake(writeSemaphore, portMAX_DELAY);
    //xSemaphoreTake(readSemaphore, portMAX_DELAY);
}
void JSRobotState::write_unlock() {
    //xSemaphoreGive(readSemaphore);
    //xSemaphoreGive(writeSemaphore);
}

extern "C" void initModuleDevice() {
    jsRobotState = JSRobotState(allBoards);
}
