using System;
using System.Collections.Generic;

using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using GameData.Domains;
using GameData.Domains.Taiwu.Profession;
using GameData.Utilities;

using GameData.Common;
using GameData.Domains.Character;
using GameData.Domains.Combat;
using GameData.Domains.LifeRecord;
using GameData.Domains.Map;


namespace TaiwuTestMod
{
	[PluginConfig("EviTestMod", "evixenon", "0.1.0")]
	public class YouYiYiZhenFix : TaiwuRemakePlugin
	{
		public static bool modOn;
		Harmony harmony;

		public override void Dispose()
		{
			if (harmony != null)
			{
				harmony.UnpatchSelf();
			}
		}

		public override void Initialize()
		{
			harmony = Harmony.CreateAndPatchAll(typeof(YouYiYiZhenFix));
		}
		public override void OnModSettingUpdate()
		{
			DomainManager.Mod.GetSetting(base.ModIdStr, "ModOn", ref modOn);
		}

		// 游医义诊会增加内息紊乱疑似bug, 试图修复
		[HarmonyPrefix, HarmonyPatch(typeof(ProfessionSkillHandle), "ExecuteOnClick_DoctorSkill_1")]
		public static bool YouYiYiZhen(DataContext context, ProfessionData professionData)
		{
			// 如果 Mod 设置为关, 直接执行原函数
			if (!modOn) return true;

			GameData.Domains.Character.Character taiwu = DomainManager.Taiwu.GetTaiwu();
			short medicineAttainment = taiwu.GetLifeSkillAttainment(8);
			short toxicologyAttainment = taiwu.GetLifeSkillAttainment(9);
			int disorderOfQiDelta = -medicineAttainment * professionData.Seniority / 5;

			// 因为后面会强制转成 short, 加一个 short 范围判定
			if (disorderOfQiDelta < -32767)
			{
				disorderOfQiDelta = -32767;
			}

			Location location = taiwu.GetLocation();
			MapBlockData settlementBlock = DomainManager.Map.GetBelongSettlementBlock(location);
			int treatCount = 0;
			int maxGrade = professionData.GetSeniorityOrgGrade();
			List<short> blockIds = new List<short>();
			DomainManager.Map.GetSettlementBlocks(settlementBlock.AreaId, settlementBlock.BlockId, blockIds);
			foreach (short blockId in blockIds)
			{
				MapBlockData block = DomainManager.Map.GetBlock(settlementBlock.AreaId, blockId);
				if (block.CharacterSet == null)
				{
					continue;
				}
				foreach (int charId2 in block.CharacterSet)
				{
					GameData.Domains.Character.Character character3 = DomainManager.Character.GetElement_Objects(charId2);
					if (character3.GetOrganizationInfo().Grade <= maxGrade && character3.GetLegendaryBookOwnerState() < 2 && TryTreatCharacter(character3))
					{
						treatCount++;
					}
				}
			}
			HashSet<int> taiwuGroup = DomainManager.Taiwu.GetGroupCharIds().GetCollection();
			foreach (int charId in taiwuGroup)
			{
				if (charId != taiwu.GetId())
				{
					GameData.Domains.Character.Character character2 = DomainManager.Character.GetElement_Objects(charId);
					if (TryTreatCharacter(character2))
					{
						treatCount++;
					}
				}
			}
			taiwu.RecordFameAction(context, 24, -1, 1);
			LifeRecordCollection lifeRecordCollection = DomainManager.LifeRecord.GetLifeRecordCollection();
			int currDate = DomainManager.World.GetCurrDate();
			lifeRecordCollection.AddFreeMedicalConsultation(taiwu.GetId(), currDate, location);
			int taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
			DomainManager.World.GetInstantNotificationCollection().AddProfessionDoctorSkill1(taiwuId, location, treatCount);
			bool TryTreatCharacter(GameData.Domains.Character.Character character)
			{
				Injuries injuries = character.GetInjuries();
				PoisonInts poisons = character.GetPoisoned();
				short disorderOfQi = character.GetDisorderOfQi();
				if (disorderOfQi <= 0 && !poisons.IsNonZero() && injuries.GetSum() <= 0)
				{
					return false;
				}
				Injuries healedInjuries = CombatDomain.HealInjury(injuries, medicineAttainment).Item1;
				character.SetInjuries(healedInjuries, context);
				PoisonInts poisonResists = character.GetPoisonResists();
				PoisonInts result = CombatDomain.HealPoison(character.GetId(), poisons, poisonResists, toxicologyAttainment).Item1;
				character.SetPoisoned(ref result, context);
				character.ChangeDisorderOfQi(context, (short)disorderOfQiDelta);
				int spiritualDebt = 10 * (character.GetOrganizationInfo().Grade + 1);
				DomainManager.Map.SetSpiritualDebtByChange(context, location.AreaId, (short)spiritualDebt);
				DomainManager.Character.ChangeFavorability(context, character, taiwu, 6000);
				return true;
			}
			return false;
		}
	}
}
