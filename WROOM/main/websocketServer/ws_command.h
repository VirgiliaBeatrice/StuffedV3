/**
 * enums for ws communication
 */

#include <iostream>
#include <string>
#include <sstream>

enum PacketId {
    PI_NONE = 0,
    PI_JSFILE = 1,
    PI_COMMAND = 2,
    PI_SETTINGS = 3,
    PI_PINGPONG = 4,
    PI_OTA = 5
};

enum PacketSettingsId {
    PSI_NONE = 0,
    PSI_OFFLINE_MODE = 1,
    PSI_FIRMWARE_INFO = 2,
    PSI_WRITE_NVS = 3,
    PSI_READ_NVS = 4
};

enum DataType {
    DT_U8 = 0,
    DT_I8 = 1,
    DT_U16 = 2,
    DT_I16 = 3,
    DT_U32 = 4,
    DT_I32 = 5,
    DT_STR = 6
};

inline std::string getPacketIdStr(int id){
    switch (id)
    {
        case PacketId::PI_NONE:
            return "PI_NONE";
        case PacketId::PI_JSFILE:
            return "PI_JSFILE";
        case PacketId::PI_COMMAND:
            return "PI_COMMAND";
        case PacketId::PI_SETTINGS:
            return "PI_SETTINGS";
        case PacketId::PI_PINGPONG:
            return "PI_PINGPONG";
        case PacketId::PI_OTA:
            return "PI_OTA";
        default:
            return "UNRECOGNIZED";
    }
}

inline std::string getPacketSettingsIdStr(int id){
    switch (id)
    {
        case PacketSettingsId::PSI_OFFLINE_MODE:
            return "PSI_OFFLINE_MODE";
        case PacketSettingsId::PSI_FIRMWARE_INFO:
            return "PSI_FIRMWARE_INFO";
        case PacketSettingsId::PSI_WRITE_NVS:
            return "PSI_WRITE_NVS";
        case PacketSettingsId::PSI_READ_NVS:
            return "PSI_READ_NVS";
        default:
            return "UNRECOGNIZED";
    }
}
