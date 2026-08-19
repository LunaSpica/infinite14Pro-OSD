namespace BLD.WMIOperation;

public enum WMISystemPerMode : byte
{
	BalanceMode = 1,
	PerformanceMode = 0,
	QuietMode = 2,
	Unknow = byte.MaxValue
}
