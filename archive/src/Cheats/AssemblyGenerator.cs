using System;
using System.Collections.Generic;

namespace MA_FH5Trainer.Cheats.ForzaHorizon5;

/// <summary>
/// 汇编指令生成器 - 提供可读的方式来生成x86-64汇编指令字节数组
/// </summary>
public static class AssemblyGenerator
{
    /// <summary>
    /// PUSH 指令
    /// </summary>
    public static byte[] Push(byte reg)
    {
        return new[] { (byte)(0x50 + reg) };
    }

    /// <summary>
    /// POP 指令
    /// </summary>
    public static byte[] Pop(byte reg)
    {
        return new[] { (byte)(0x58 + reg) };
    }

    /// <summary>
    /// MOV RAX, imm64 - 将64位立即数移动到RAX寄存器
    /// </summary>
    public static byte[] MovRaxImm64(ulong value)
    {
        var bytes = new List<byte> { 0x48, 0xB8 };
        bytes.AddRange(BitConverter.GetBytes(value));
        return bytes.ToArray();
    }

    /// <summary>
    /// MOV R64, R64 - 寄存器间移动
    /// </summary>
    public static byte[] MovRegReg(byte dst, byte src)
    {
        return new[] { (byte)(0x48), (byte)(0x89), (byte)((src << 3) | dst) };
    }

    /// <summary>
    /// MOV [RAX], RAX
    /// </summary>
    public static byte[] MovPtrRaxRax()
    {
        return new[] { (byte)0x48, (byte)0x89, (byte)0x00 };
    }

    /// <summary>
    /// MOV RAX, [RAX]
    /// </summary>
    public static byte[] MovRaxPtrRax()
    {
        return new[] { (byte)0x48, (byte)0x8B, (byte)0x00 };
    }

    /// <summary>
    /// MOV RAX, [RAX + offset]
    /// </summary>
    public static byte[] MovRaxPtrRaxOffset(int offset)
    {
        var bytes = new List<byte> { 0x48, 0x8B, 0x40 };
        bytes.Add((byte)offset);
        return bytes.ToArray();
    }

    /// <summary>
    /// CMP [RAX], imm32
    /// </summary>
    public static byte[] CmpPtrRaxImm32(uint value)
    {
        var bytes = new List<byte> { 0x81, 0x38 };
        bytes.AddRange(BitConverter.GetBytes(value));
        return bytes.ToArray();
    }

    /// <summary>
    /// CMP byte ptr [address], imm8
    /// </summary>
    public static byte[] CmpBytePtrImm8(uint offset, byte value)
    {
        var bytes = new List<byte> { 0x80, 0x3D };
        bytes.AddRange(BitConverter.GetBytes(offset));
        bytes.Add(value);
        return bytes.ToArray();
    }

    /// <summary>
    /// TEST XMM0, XMM1
    /// </summary>
    public static byte[] TestXmm0Xmm1()
    {
        return new[] { (byte)0x0F, (byte)0x57, (byte)0xC1 };
    }

    /// <summary>
    /// JNE offset
    /// </summary>
    public static byte[] Jne(int offset)
    {
        return new[] { (byte)0x75, (byte)offset };
    }

    /// <summary>
    /// JE offset
    /// </summary>
    public static byte[] Je(int offset)
    {
        return new[] { (byte)0x74, (byte)offset };
    }

    /// <summary>
    /// JGE offset
    /// </summary>
    public static byte[] Jge(int offset)
    {
        return new[] { (byte)0x7D, (byte)offset };
    }

    /// <summary>
    /// SUB RSP, imm8
    /// </summary>
    public static byte[] SubRspImm8(byte value)
    {
        return new[] { (byte)0x48, (byte)0x83, (byte)0xEC, value };
    }

    /// <summary>
    /// ADD RSP, imm8
    /// </summary>
    public static byte[] AddRspImm8(byte value)
    {
        return new[] { (byte)0x48, (byte)0x83, (byte)0xC4, value };
    }

    /// <summary>
    /// RET
    /// </summary>
    public static byte[] Ret()
    {
        return new[] { (byte)0xC3 };
    }

    /// <summary>
    /// NOP
    /// </summary>
    public static byte[] Nop()
    {
        return new[] { (byte)0x90 };
    }

    /// <summary>
    /// MOVAPS [address], XMM0
    /// </summary>
    public static byte[] MovapsPtrXmm0(uint offset)
    {
        var bytes = new List<byte> { 0x0F, 0x29, 0x87 };
        bytes.AddRange(BitConverter.GetBytes(offset));
        return bytes.ToArray();
    }

    /// <summary>
    /// MOVSS [address], XMM0
    /// </summary>
    public static byte[] MovssPtrXmm0(uint offset)
    {
        var bytes = new List<byte> { 0xF3, 0x0F, 0x11, 0x87 };
        bytes.AddRange(BitConverter.GetBytes(offset));
        return bytes.ToArray();
    }

    /// <summary>
    /// MOVSS XMM0, [address]
    /// </summary>
    public static byte[] MovssXmm0Ptr(uint offset)
    {
        var bytes = new List<byte> { 0xF3, 0x0F, 0x10, 0x87 };
        bytes.AddRange(BitConverter.GetBytes(offset));
        return bytes.ToArray();
    }

    /// <summary>
    /// MOVSS XMM1, [address]
    /// </summary>
    public static byte[] MovssXmm1Ptr(uint offset)
    {
        var bytes = new List<byte> { 0xF3, 0x0F, 0x10, 0x8F };
        bytes.AddRange(BitConverter.GetBytes(offset));
        return bytes.ToArray();
    }

    /// <summary>
    /// MULSS XMM0, XMM1
    /// </summary>
    public static byte[] MulssXmm0Xmm1()
    {
        return new[] { (byte)0xF3, (byte)0x0F, (byte)0x59, (byte)0xC1 };
    }

    /// <summary>
    /// MULSS XMM0, [address]
    /// </summary>
    public static byte[] MulssXmm0Ptr(uint offset)
    {
        var bytes = new List<byte> { 0xF3, 0x0F, 0x59, 0x87 };
        bytes.AddRange(BitConverter.GetBytes(offset));
        return bytes.ToArray();
    }

    /// <summary>
    /// ADDSS XMM0, XMM1
    /// </summary>
    public static byte[] AddssXmm0Xmm1()
    {
        return new[] { (byte)0xF3, (byte)0x0F, (byte)0x58, (byte)0xC1 };
    }

    /// <summary>
    /// ADDSS XMM0, [address]
    /// </summary>
    public static byte[] AddssXmm0Ptr(uint offset)
    {
        var bytes = new List<byte> { 0xF3, 0x0F, 0x58, 0x87 };
        bytes.AddRange(BitConverter.GetBytes(offset));
        return bytes.ToArray();
    }

    /// <summary>
    /// DIVSS XMM0, XMM1
    /// </summary>
    public static byte[] DivssXmm0Xmm1()
    {
        return new[] { (byte)0xF3, (byte)0x0F, (byte)0x5E, (byte)0xC1 };
    }

    /// <summary>
    /// COMISS XMM0, [address]
    /// </summary>
    public static byte[] ComissXmm0Ptr(uint offset)
    {
        var bytes = new List<byte> { 0x0F, 0x2F, 0x87 };
        bytes.AddRange(BitConverter.GetBytes(offset));
        return bytes.ToArray();
    }

    /// <summary>
    /// 生成本地玩家钩子汇编代码
    /// </summary>
    public static byte[] GenerateLocalPlayerHook(ulong racePtr)
    {
        var asm = new List<byte>();
        
        // Push rbx
        asm.Add(0x53);
        
        // sub rsp, 0x30
        asm.AddRange(SubRspImm8(0x30));
        
        // movaps [rsp], xmm0
        asm.AddRange(new[] { (byte)0x0F, (byte)0x29, (byte)0x04, (byte)0x24 });
        
        // movaps [rsp+0x10], xmm1
        asm.AddRange(new[] { (byte)0x0F, (byte)0x29, (byte)0x4C, (byte)0x24, (byte)0x10 });
        
        // movaps [rsp+0x20], xmm2
        asm.AddRange(new[] { (byte)0x0F, (byte)0x29, (byte)0x54, (byte)0x24, (byte)0x20 });
        
        // mov rax, racePtr
        asm.AddRange(MovRaxImm64(racePtr));
        
        // mov rax, [rax]
        asm.AddRange(MovRaxPtrRax());
        
        // mov rax, [rax+0x50]
        asm.AddRange(MovRaxPtrRaxOffset(0x50));
        
        // mov rax, [rax+0x3D8]
        asm.AddRange(new[] { (byte)0x48, (byte)0x8D, (byte)0x80, (byte)0xD8, (byte)0x03, (byte)0x00, (byte)0x00 });
        
        // cmp [rax], 0x3F800000
        asm.AddRange(CmpPtrRaxImm32(0x3F800000));
        
        // je ... (后续指令继续)
        // 注意：实际实现需要完整的汇编逻辑
        
        return asm.ToArray();
    }
}
