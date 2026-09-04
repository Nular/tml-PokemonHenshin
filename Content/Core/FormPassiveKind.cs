namespace PokemonHenshin.Content.Core
{
	/// <summary>持握被动种类（Excel → 泰拉适配，见 docs/move-effects.md）。</summary>
	public enum FormPassiveKind : byte
	{
		None = 0,
		Blaze,          // 猛火：低生命火伤↑
		Torrent,        // 激流：低生命水伤↑
		Overgrow,       // 茂盛：低生命草伤↑
		Chlorophyll,    // 叶绿素：白天移速↑
		RainDish,       // 雨盘：雨/夜回血
		Synchronize,    // 同步：受异常反弹
		ShedSkin,       // 蜕皮：概率清 debuff
		Multiscale,     // 多重鳞片：满血减伤
		ClearBody,      // 恒净之躯：免疫 debuff
		RoughSkin,      // 粗糙皮肤：受击反伤
		Levitate,       // 飘浮：强飞行
		SwiftSwim,      // 优游自如：雨/夜移速
		Moxie,          // 自信过度：击杀叠攻
		Guts,           // 毅力：有 debuff 时增伤
		KeenEye,        // 锐利目光：伤害×1.2
		Static,         // 静电：被近战接触反麻
		RockHead,       // 坚硬脑袋：免疫反伤 + 减伤
		Pressure,       // 压迫感：伤害×1.5
		AirLock,        // 气闸：伤害×1.7
		SolarPower,     // 太阳之力：白天增伤但每次攻击扣血
		SandVeil        // 沙隐（地鼠补全）：挖速/地下移速
	}
}
