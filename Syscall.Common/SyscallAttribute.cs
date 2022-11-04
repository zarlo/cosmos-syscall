namespace Syscall.Common;

[System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)] 
public class SyscallAttribute: System.Attribute
{
    public uint[] SysCall {get; private set;}

    public SyscallAttribute(
        uint[] SysCall
        )
    {

        this.SysCall = SysCall;
    }
    public SyscallAttribute(
        uint SysCall
        )
    {

        this.SysCall = new uint[] { SysCall };
    }

}