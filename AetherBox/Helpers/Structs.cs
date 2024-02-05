using System.Numerics;
using System.Runtime.InteropServices;
using AetherBox.Helpers;

namespace AetherBox.Helpers;

public static class Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct PlayerController
    {
        [FieldOffset(16)]
        public PlayerMoveControllerWalk MoveControllerWalk;

        [FieldOffset(336)]
        public PlayerMoveControllerFly MoveControllerFly;

        [FieldOffset(1369)]
        public byte ControlMode;
    }

    [StructLayout(LayoutKind.Explicit, Size = 320)]
    public struct PlayerMoveControllerWalk
    {
        [FieldOffset(16)]
        public Vector3 MovementDir;

        [FieldOffset(88)]
        public float BaseMovementSpeed;

        [FieldOffset(144)]
        public float MovementDirRelToCharacterFacing;

        [FieldOffset(148)]
        public byte Forced;

        [FieldOffset(160)]
        public Vector3 MovementDirWorld;

        [FieldOffset(176)]
        public float RotationDir;

        [FieldOffset(272)]
        public uint MovementState;

        [FieldOffset(276)]
        public float MovementLeft;

        [FieldOffset(280)]
        public float MovementFwd;
    }

    [StructLayout(LayoutKind.Explicit, Size = 176)]
    public struct PlayerMoveControllerFly
    {
        [FieldOffset(16)]
        public float unk10;

        [FieldOffset(20)]
        public float unk14;

        [FieldOffset(24)]
        public float unk18;

        [FieldOffset(64)]
        public float unk40;

        [FieldOffset(68)]
        public float unk44;

        [FieldOffset(72)]
        public uint unk48;

        [FieldOffset(76)]
        public uint unk4C;

        [FieldOffset(80)]
        public uint unk50;

        [FieldOffset(88)]
        public float unk58;

        [FieldOffset(92)]
        public float unk5C;

        [FieldOffset(102)]
        public byte IsFlying;

        [FieldOffset(136)]
        public uint unk88;

        [FieldOffset(140)]
        public uint unk8C;

        [FieldOffset(144)]
        public uint unk90;

        [FieldOffset(148)]
        public float unk94;

        [FieldOffset(152)]
        public float unk98;

        [FieldOffset(156)]
        public float AngularAscent;
    }

    [StructLayout(LayoutKind.Explicit, Size = 688)]
    public struct CameraEx
    {
        [FieldOffset(304)]
        public float DirH;

        [FieldOffset(308)]
        public float DirV;

        [FieldOffset(312)]
        public float InputDeltaHAdjusted;

        [FieldOffset(316)]
        public float InputDeltaVAdjusted;

        [FieldOffset(320)]
        public float InputDeltaH;

        [FieldOffset(324)]
        public float InputDeltaV;

        [FieldOffset(328)]
        public float DirVMin;

        [FieldOffset(332)]
        public float DirVMax;
    }

    [StructLayout(LayoutKind.Explicit, Size = 7120)]
    public struct Character
    {
        [FieldOffset(1548)]
        public byte IsFlying;
    }
}
