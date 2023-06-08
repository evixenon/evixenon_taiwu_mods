using System;
using System.Collections.Generic;

using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using GameData.Domains;
using GameData.Utilities;

using System.Linq;
using GameData.Common;
using GameData.DomainEvents;
using GameData.Domains.Character;
using GameData.Domains.CombatSkill;
using GameData.Domains.Item;
using GameData.Serializer;
using GameData.Domains.LifeRecord;
using GameData.Domains.Map;
using static GameData.DomainEvents.Events;
using static GameData.Domains.Taiwu.TaiwuDomain;
using Config.EventConfig;
using static Config.EventConfig.TaiwuEventItem;

using GameData.Domains.Taiwu;
using GameData.Domains.TaiwuEvent;


namespace TaiwuTestMod
{
	[PluginConfig("EviTestMod", "evixenon", "0.1.0")]
	public class EviTestMod : TaiwuRemakePlugin
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
			harmony = Harmony.CreateAndPatchAll(typeof(EviTestMod));
		}
		public override void OnModSettingUpdate()
		{
			DomainManager.Mod.GetSetting(base.ModIdStr, "ModOn", ref modOn);
		}

		// Log函数
		public static void eviLog(string s)
		{
			AdaptableLog.Info("[=EviTest=] " + s);
		}

		// 入魔事件
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Events), "RaiseXiangshuInfectionFeatureChanged")]
		public static bool RaiseXiangshuInfectionFeatureChangedPrefix(DataContext context, Character character, short featureId)
		{
			int taiwuCharId = DomainManager.Taiwu.GetTaiwuCharId();
			int charId = character.GetId();
			// 入魔
			if (featureId == 218)
			{
				AdaptableLog.Info("RaiseXiangshuInfectionFeatureChanged 218");
				int leaderId = character.GetLeaderId();
				int kidnapperId = character.GetKidnapperId();
				if (!character.GetLocation().IsValid())
				{
					if ((leaderId < 0 || charId == leaderId) && DomainManager.Character.TryGetElement_CrossAreaMoveInfos(leaderId, out var crossAreaMoveInfo))
					{
						Location validLocation2 = DomainManager.Map.CrossAreaTravelInfoToLocation(crossAreaMoveInfo);
						DomainManager.Character.RemoveCrossAreaTravelInfo(context, charId);
						DomainManager.Character.GroupMove(context, character, validLocation2);
					}
					else if (kidnapperId >= 0)
					{
						DomainManager.Character.RemoveKidnappedCharacter(context, charId, kidnapperId, isEscaped: true);
					}
					else
					{
						Location validLocation = character.GetValidLocation();
						character.SetLocation(validLocation, context);
					}
				}

				// 前面的逻辑应该改过taiwuId了, 所以不能直接判断
				// bool isTaiwu = character.GetId() == DomainManager.Taiwu.GetTaiwuCharId();
				bool isTaiwu = character.GetOrganizationInfo().Equals(DomainManager.Taiwu.GetTaiwu().GetOrganizationInfo());
				bool isActivelyPass = true;
				
				if (isTaiwu && isActivelyPass)
                {
					eviLog("是入魔太吾");
                    DomainManager.Character.LeaveGroup(context, character, bringWards: false);
					eviLog("orgTemplatedId:" + character.GetOrganizationInfo().OrgTemplateId.ToString());
					eviLog("grade:" + character.GetOrganizationInfo().Grade.ToString());
					OrganizationInfo destOrgInfo1 = character.GetOrganizationInfo();
					destOrgInfo1.Grade = 0;
					destOrgInfo1.Principal = true;
					DomainManager.Organization.ChangeOrganization(context, character, destOrgInfo1);
					character.SetOrganizationInfo(destOrgInfo1, context);
					return false;
                }
				DomainManager.Character.LeaveGroup(context, character, bringWards: false);
				OrganizationInfo destOrgInfo = new OrganizationInfo(20, character.GetOrganizationInfo().Grade, principal: true, -1); // 相枢爪牙, 当前阶级, principal, 无所属地区
				DomainManager.Organization.ChangeOrganization(context, character, destOrgInfo);
				character.SetOrganizationInfo(destOrgInfo, context);
				DomainManager.Character.AddInfectedCharToSet(charId);
				Location location3 = character.GetLocation();
				int date3 = DomainManager.World.GetCurrDate();
				DomainManager.LifeRecord.GetLifeRecordCollection().AddXiangshuCompletelyInfected(charId, date3, location3);
				if (DomainManager.Character.IsTaiwuPeople(charId))
				{
					DomainManager.World.GetMonthlyNotificationCollection().AddInfectXiangshuCompletely(charId, location3);
				}
				RaiseCharacterLocationChanged(context, charId, location3, Location.Invalid);
				RaiseInfectedCharacterLocationChanged(context, charId, Location.Invalid, location3);
			}
			else if (character.GetOrganizationInfo().OrgTemplateId == 20)
			{
				Location location2 = character.GetLocation();
				if (!location2.IsValid())
				{
					location2 = character.GetValidLocation();
				}
				int date2 = DomainManager.World.GetCurrDate();
				LifeRecordCollection lifeRecordCollection = DomainManager.LifeRecord.GetLifeRecordCollection();
				if (character.IsActiveExternalRelationState(8))
				{
					lifeRecordCollection.AddSavedFromInfection(charId, date2, taiwuCharId, DomainManager.Taiwu.GetTaiwuVillageLocation());
				}
				else
				{
					lifeRecordCollection.AddSavedFromInfection(charId, date2, taiwuCharId, location2);
				}
				DomainManager.Organization.JoinNearbyVillageTownAsBeggar(context, character, -1);
				DomainManager.Character.RemoveInfectedCharFromSet(charId);
				RaiseInfectedCharacterLocationChanged(context, charId, location2, Location.Invalid);
				RaiseCharacterLocationChanged(context, charId, Location.Invalid, location2);
			}
			else if (featureId == 217)
			{
				// 入邪?
				Location location = character.GetLocation();
				int date = DomainManager.World.GetCurrDate();
				DomainManager.LifeRecord.GetLifeRecordCollection().AddXiangshuPartiallyInfected(charId, date, location);
				if (DomainManager.Character.IsTaiwuPeople(charId))
				{
					DomainManager.World.GetMonthlyNotificationCollection().AddInfectXiangshuPartially(charId, location);
				}
			}
			return false;
		}

/*
		[HarmonyPrefix]
		[HarmonyPatch(typeof(TaiwuDomain), "TransferTaiwuData")]
		public static void TransferTaiwuDataPrefix(DataContext context, GameData.Domains.Character.Character newTaiwuChar, GameData.Domains.Character.Character oldTaiwuChar, bool randomSuccessor = false)
		{

			int newTaiwuCharId = newTaiwuChar.GetId();
			int oldTaiwuCharId = oldTaiwuChar.GetId();
			DomainManager.Character.PrepareTaiwuInheritor(context, newTaiwuChar, oldTaiwuChar);
			TaiwuDomain.SetTaiwuGenerationsCount(_taiwuGenerationsCount + 1, context); ///
			List<short> learnedCombatSkills = newTaiwuChar.GetLearnedCombatSkills();
			List<GameData.Domains.CombatSkill.CombatSkill> combatSkills = ObjectPool<List<GameData.Domains.CombatSkill.CombatSkill>>.Instance.Get();
			combatSkills.Clear();
			foreach (short combatSkillId in oldTaiwuChar.GetLearnedCombatSkills())
			{
				GameData.Domains.CombatSkill.CombatSkill oldSkill = DomainManager.CombatSkill.GetElement_CombatSkills(new CombatSkillKey(oldTaiwuCharId, combatSkillId));
				GameData.Domains.CombatSkill.CombatSkill newSkill = GameData.Serializer.Serializer.CreateCopy(oldSkill);
				newSkill.OfflineSetCharId(newTaiwuCharId);
				combatSkills.Add(newSkill);
				learnedCombatSkills.Add(combatSkillId);
			}
			DomainManager.CombatSkill.RegisterCombatSkills(newTaiwuCharId, combatSkills);
			ObjectPool<List<GameData.Domains.CombatSkill.CombatSkill>>.Instance.Return(combatSkills);
			newTaiwuChar.SetLearnedCombatSkills(learnedCombatSkills, context);
			newTaiwuChar.SetEquippedCombatSkills(oldTaiwuChar.GetEquippedCombatSkills().ToArray(), context);
			DomainManager.SpecialEffect.AddAllBrokenSkillEffects(context, newTaiwuChar);
			newTaiwuChar.SetLoopingNeigong(oldTaiwuChar.GetLoopingNeigong(), context);
			newTaiwuChar.SetCombatSkillAttainmentPanels(oldTaiwuChar.GetCombatSkillAttainmentPanels().ToArray(), context);
			List<LifeSkillItem> learnedLifeSkills = newTaiwuChar.GetLearnedLifeSkills();
			learnedLifeSkills.Clear();
			learnedLifeSkills.AddRange(oldTaiwuChar.GetLearnedLifeSkills());
			newTaiwuChar.SetLearnedLifeSkills(learnedLifeSkills, context);
			ItemKey[] emptyEquipments = new ItemKey[12];
			for (int j = 0; j < 12; j++)
			{
				emptyEquipments[j] = ItemKey.Invalid;
			}
			ItemKey[] originalTaiwuEquipment = oldTaiwuChar.GetEquipment().ToArray();
			emptyEquipments[4] = originalTaiwuEquipment[4];
			oldTaiwuChar.ChangeEquipment(context, emptyEquipments);
			Dictionary<ItemKey, int> inventoryItems = oldTaiwuChar.GetInventory().Items;
			foreach (KeyValuePair<ItemKey, int> item in inventoryItems)
			{
				newTaiwuChar.AddInventoryItem(context, item.Key, item.Value);
			}
			oldTaiwuChar.SetInventory(new Inventory(), context);
			ResetEquipmentPlans(context); ///
			for (sbyte i = 0; i < 8; i = (sbyte)(i + 1))
			{
				newTaiwuChar.SpecifyResource(context, i, oldTaiwuChar.GetResource(i));
				oldTaiwuChar.SpecifyResource(context, i, 0);
			}
			newTaiwuChar.SetExp(oldTaiwuChar.GetExp(), context);
			DomainManager.Information.TransferInformation(oldTaiwuCharId, newTaiwuCharId, context);
			if (randomSuccessor)
			{
				HashSet<int> groupCharSet = ObjectPool<HashSet<int>>.Instance.Get();
				groupCharSet.Clear();
				groupCharSet.UnionWith(_groupCharIds.GetCollection());
				_groupCharIds.Remove(oldTaiwuCharId);
				foreach (int charId4 in _groupCharIds.GetCollection())
				{
					if (DomainManager.Character.GetElement_Objects(charId4).GetAgeGroup() != 0)
					{
						groupCharSet.Add(charId4);
					}
				}
				foreach (int charId3 in groupCharSet)
				{
					if (charId3 != oldTaiwuCharId && _groupCharIds.Contains(charId3))
					{
						TaiwuDomain.LeaveGroup(context, charId3, bringWards: true, showNotification: false); ///
					}
				}
				_groupCharIds.Add(oldTaiwuCharId);
				ObjectPool<HashSet<int>>.Instance.Get();
				foreach (int charId2 in _groupCharIds.GetCollection())
				{
					if (charId2 != oldTaiwuCharId)
					{
						LeaveGroup(context, charId2, bringWards: true, showNotification: false);
					}
				}
				JoinGroup(context, newTaiwuCharId, showNotification: false);
				oldTaiwuChar.SetLeaderId(newTaiwuCharId, context);
				newTaiwuChar.SetLeaderId(newTaiwuCharId, context);
			}
			else
			{
				foreach (int charId in _groupCharIds.GetCollection())
				{
					GameData.Domains.Character.Character character = DomainManager.Character.GetElement_Objects(charId);
					character.SetLeaderId(newTaiwuCharId, context);
				}
				DomainManager.Character.TransferKidnappedCharactersToTaiwuSuccessor(context, newTaiwuChar, oldTaiwuChar);
			}
			if (randomSuccessor)
			{
				newTaiwuChar.SetHappiness(50, context);
				List<FameActionRecord> fameActionRecords = newTaiwuChar.GetFameActionRecords();
				fameActionRecords.Clear();
				newTaiwuChar.SetFameActionRecords(fameActionRecords, context);
			}
			DomainManager.Organization.ResetSectExploreStatuses(context);
			DomainManager.TaiwuEvent.ClearTaiwuBindingMonthlyActions(context);
			DomainManager.World.GetMonthlyEventCollection().Clear();
			DomainManager.World.TransferXiangshuAvatarRelations(context, oldTaiwuCharId, newTaiwuCharId);
			DomainManager.Organization.ResetSectExploreStatuses(context);
			DomainManager.Extra.RemoveAllTaiwuExtraBonuses(context);
			DomainManager.Building.SetShrineBuyTimes(0, context);
			ClearPrenatalEducationBonus(context);
			newTaiwuChar.SetCurrNeili(oldTaiwuChar.GetCurrNeili(), context);
			newTaiwuChar.SetExtraNeili(oldTaiwuChar.GetExtraNeili(), context);
			newTaiwuChar.SetBaseNeiliAllocation(oldTaiwuChar.GetBaseNeiliAllocation(), context);
			newTaiwuChar.SetConsummateLevel(oldTaiwuChar.GetConsummateLevel(), context);
			foreach (short selectedLegacyId in _selectedLegacies)
			{
				ApplyLegacyAffect(context, newTaiwuChar, selectedLegacyId);
			}
			ResetLegacy(context);
			OrganizationInfo taiwuOrgInfo = oldTaiwuChar.GetOrganizationInfo();
			if (_isTaiwuDying)
			{
				LeaveGroup(context, oldTaiwuCharId, bringWards: false, showNotification: false);
				DomainManager.Character.Die(context, oldTaiwuChar, DomainManager.World.GetAdvancingMonthState() != 0);
				DomainManager.Organization.ChangeGrade(context, newTaiwuChar, taiwuOrgInfo.Grade, taiwuOrgInfo.Principal);
			}
			else
			{
				LeaveGroup(context, oldTaiwuCharId, bringWards: false);
				Events.RaiseXiangshuInfectionFeatureChanged(context, oldTaiwuChar, 218);
				DomainManager.Organization.ChangeGrade(context, newTaiwuChar, taiwuOrgInfo.Grade, taiwuOrgInfo.Principal);
			}
		}

		*/
	}
}
