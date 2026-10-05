using NeoRuneExtended.Assets;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.UnrealTypes;

internal static class Hello
{
	public const string Mod = "NeoRuneHello";

	public static void Build(string assetsDir)
	{
		string text = "/Game/Mods/NeoRuneHello";
		BlueprintBuilder blueprintBuilder = new BlueprintBuilder(text + "/NeoRuneLog", "NeoRuneLog", "/Script/Engine.SaveGame");
		blueprintBuilder.AddVariable("Text", UType.String);
		blueprintBuilder.Write(assetsDir);
		BlueprintBuilder blueprintBuilder2 = new BlueprintBuilder(text + "/ModActor", "ModActor", "/Script/Engine.Actor");
		PackageBuilder package = blueprintBuilder2.Package;
		FunctionBuilder functionBuilder = blueprintBuilder2.AddFunction("ReceiveBeginPlay", EFunctionFlags.FUNC_Event | EFunctionFlags.FUNC_Protected | EFunctionFlags.FUNC_BlueprintEvent, "/Script/Engine.Actor:ReceiveBeginPlay");
		functionBuilder.AddLocal("Save", new UType.Object("/Script/Engine.SaveGame"));
		ScriptBuilder script = functionBuilder.Script;
		FPackageIndex fPackageIndex = package.ImportClass(text + "/NeoRuneLog.NeoRuneLog_C");
		script.Emit(script.Let(new UType.Object("/Script/Engine.SaveGame"), script.Local("Save"), script.CallMath(package.ImportFunction("/Script/Engine.GameplayStatics:CreateSaveGameObject"), script.Object(fPackageIndex)), script.Pointer("Save", functionBuilder.Index)));
		KismetPropertyPointer kismetPropertyPointer = script.Pointer("Text", fPackageIndex);
		script.Emit(script.Let(UType.String, script.Context(script.Local("Save"), script.Member("Text", fPackageIndex), kismetPropertyPointer), script.CallMath(package.ImportFunction("/Script/Engine.KismetStringLibrary:Concat_StrStr"), ScriptBuilder.Str("Hello from NeoRune! Level="), script.CallMath(package.ImportFunction("/Script/Engine.GameplayStatics:GetCurrentLevelName"), ScriptBuilder.Self(), ScriptBuilder.Bool(v: true))), kismetPropertyPointer));
		script.Emit(script.CallMath(package.ImportFunction("/Script/Engine.GameplayStatics:SaveGameToSlot"), script.Local("Save"), ScriptBuilder.Str("NeoRuneHello"), ScriptBuilder.Int(0)));
		blueprintBuilder2.Write(assetsDir);
	}
}
