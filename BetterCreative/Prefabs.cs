﻿
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace Heinermann.BetterCreative
{
  internal static class Prefabs
  {
    // Refs:
    //  - ObjectDB.m_items
    //  - ItemDrop.m_itemData.m_shared.m_buildPieces
    //  - PieceTable.m_pieces
    private static HashSet<string> GetPieceNames()
    {
      HashSet<string> result = new HashSet<string>();

      foreach (var item in ObjectDB.instance.m_items)
      {
        var table = item.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces;
        if (table == null) continue;

        foreach (var piece in table.m_pieces)
        {
          result.Add(piece.name);
        }
      }

      return result;
    }

    private static readonly HashSet<string> IgnoredPrefabs = new HashSet<string>() {
      "Player", "Valkyrie", "odin", "CargoCrate", "CastleKit_pot03", "Pickable_Item"
    };

    private static bool HasAnyComponent(GameObject prefab, params string[] components)
    {
      foreach (string component in components)
      {
        if (prefab.GetComponent(component) != null) return true;
      }
      return false;
    }

    private static bool ShouldIgnorePrefab(GameObject prefab)
    {
      HashSet<string> prefabsToSkip = GetPieceNames();

      return
        HasAnyComponent(prefab, "ItemDrop", "Projectile", "TimedDestruction", "Ragdoll", "Plant", "Fish", "FishingFloat", "RandomFlyingBird", "DungeonGenerator", "ZSFX", "MusicLocation", "LocationProxy", "MineRock5", "LootSpawner", "TombStone") ||
        (prefab.GetComponent("Aoe") != null && prefab.GetComponent("WearNTear") == null) ||
        (prefab.GetComponent("TerrainModifier") != null && prefab.GetComponent("Destructible") == null) ||
        prefab.name.StartsWith("vfx_") ||
        prefab.name.StartsWith("sfx_") ||
        prefab.name.StartsWith("fx_") ||
        prefab.name.StartsWith("_") ||
        IgnoredPrefabs.Contains(prefab.name) ||
        prefabsToSkip.Contains(prefab.name);
    }

    private static string GetPrefabCategory(GameObject prefab)
    {
      string category = "Other";
      if (prefab.GetComponent("Location"))
      {
        category = "Locations";
      }
      else if (HasAnyComponent(prefab, "Pickable", "PickableItem"))
      {
        category = "Pickable";
      }
      else if (HasAnyComponent(prefab, "Humanoid", "Character", "Leviathan"))
      {
        category = "Monsters";
      }
      else if (HasAnyComponent(prefab, "CreatureSpawner", "SpawnArea"))
      {
        category = "Spawners";
      }
      else if (prefab.name.Contains("Tree") ||
        prefab.name.Contains("Oak") ||
        prefab.name.Contains("Pine") ||
        prefab.name.Contains("Beech") ||
        prefab.name.Contains("Birch") ||
        prefab.name.Contains("Bush") ||
        prefab.name.Contains("Root") ||
        prefab.name.Contains("root") ||
        prefab.name.Contains("shrub") ||
        prefab.name.Contains("stubbe") ||
        HasAnyComponent(prefab, "TreeBase", "TreeLog"))
      {
        category = "Vegetation";
      }
      else if (prefab.GetComponent("WearNTear"))
      {
        category = "Building 2";
      }
      else if (HasAnyComponent(prefab, "Destructible", "MineRock"))
      {
        category = "Destructible";
      }
      return category;
    }

    private static readonly HashSet<string> unrestrictedExceptions = new HashSet<string>()
    {
      "GlowingMushroom", "Flies", "horizontal_web", "tunnel_web", "rockformation1", "StatueCorgi", "StatueDeer", "StatueEvil", "StatueHare", "StatueSeed"
    };

    // Refs:
    // - Tons of members of Piece
    private static void ModifyPiece(Piece piece, bool new_piece)
    {
      if (piece == null) return;

      piece.m_enabled = true;
      piece.m_canBeRemoved = true;

      if (BetterCreative.UnrestrictedPlacement.Value ||
        HasAnyComponent(piece.gameObject, "Humanoid", "Character", "Destructible", "TreeBase", "MeshCollider", "LiquidVolume", "Pickable", "PickableItem") ||
        unrestrictedExceptions.Contains(piece.name))
      {
        piece.m_clipEverything = piece.GetComponent("Floating") == null && new_piece;
      }
      
      if (BetterCreative.UnrestrictedPlacement.Value)
      {
        piece.m_groundPiece = false;
        piece.m_groundOnly = false;
        piece.m_noInWater = false;
        piece.m_notOnWood = false;
        piece.m_notOnTiltingSurface = false;
        piece.m_notOnFloor = false;
        piece.m_allowedInDungeons = true;
        piece.m_onlyInTeleportArea = false;
        piece.m_inCeilingOnly = false;
        piece.m_cultivatedGroundOnly = false;
        piece.m_onlyInBiome = Heightmap.Biome.None;
        piece.m_allowRotatedOverlap = true;
      }
    }

    private static void InitPieceData(GameObject prefab)
    {
      Piece piece = prefab.GetComponent<Piece>();
      bool is_new_piece = false;
      if (piece == null)
      {
        piece = prefab.AddComponent<Piece>();
        is_new_piece = true;
      }
      ModifyPiece(piece, is_new_piece);
    }

    // Refs:
    //  - CreatureSpawner.m_creaturePrefab
    //  - PickableItem.m_randomItemPrefabs
    //  - PickableItem.RandomItem.m_itemPrefab
    private static Sprite CreatePrefabIcon(GameObject prefab)
    {
      Sprite result = RenderManager.Instance.Render(prefab, RenderManager.IsometricRotation);
      if (result == null)
      {
        GameObject spawnedCreaturePrefab = prefab.GetComponent<CreatureSpawner>()?.m_creaturePrefab;
        if (spawnedCreaturePrefab != null)
          result = RenderManager.Instance.Render(spawnedCreaturePrefab, RenderManager.IsometricRotation);
      }

      if (result == null)
      {
        PickableItem.RandomItem[] randomItemPrefabs = prefab.GetComponent<PickableItem>()?.m_randomItemPrefabs;
        if (randomItemPrefabs != null && randomItemPrefabs.Length > 0)
        {
          GameObject item = randomItemPrefabs[0].m_itemPrefab?.gameObject;
          if (item != null)
            result = RenderManager.Instance.Render(item, RenderManager.IsometricRotation);
        }
      }
      return result;
    }

    private static void DestroyComponents<T>(GameObject go)
    {
      var components = go.GetComponentsInChildren<T>();
      foreach (var component in components)
      {
        Object.DestroyImmediate(component as UnityEngine.Object);
      }
    }

    // Refs:
    //  - DestroyComponents calls below
    //  - 
    private static void CreateGhostPrefab(GameObject prefab)
    {
      GameObject ghost = PrefabManager.Instance.CreateClonedPrefab(prefab.name + "_ghostfab", prefab);

      DestroyComponents<TreeLog>(ghost);
      DestroyComponents<TreeBase>(ghost);
      DestroyComponents<BaseAI>(ghost);
      DestroyComponents<MineRock>(ghost);
      DestroyComponents<CharacterDrop>(ghost);
      DestroyComponents<Character>(ghost);
      DestroyComponents<CharacterAnimEvent>(ghost);
      DestroyComponents<Humanoid>(ghost);
      DestroyComponents<HoverText>(ghost);
      DestroyComponents<FootStep>(ghost);
      DestroyComponents<VisEquipment>(ghost);
      DestroyComponents<ZSyncAnimation>(ghost);
      DestroyComponents<TerrainModifier>(ghost);
      DestroyComponents<GuidePoint>(ghost);
      DestroyComponents<Light>(ghost);
      DestroyComponents<LightFlicker>(ghost);
      DestroyComponents<LightLod>(ghost);
      DestroyComponents<LevelEffects>(ghost);
      DestroyComponents<AudioSource>(ghost);
      DestroyComponents<ZSFX>(ghost);
      DestroyComponents<Windmill>(ghost);
      DestroyComponents<ParticleSystem>(ghost);
      DestroyComponents<Tameable>(ghost);
      DestroyComponents<Procreation>(ghost);
      DestroyComponents<Growup>(ghost);
      DestroyComponents<SpawnArea>(ghost);
      DestroyComponents<CreatureSpawner>(ghost);
      DestroyComponents<Aoe>(ghost);
      DestroyComponents<ZSyncTransform>(ghost);
      DestroyComponents<RandomSpawn>(ghost);
      DestroyComponents<Animator>(ghost);

      // Not sure how to resolve the issue where you can't place stuff on structures.
      // So let's do some jank ass hack to work around it :)
      var chair = GameObject.Instantiate(PrefabManager.Instance.GetPrefab("piece_chair"), ghost.transform, false);
      DestroyComponents<MeshRenderer>(chair);
      DestroyComponents<ZNetView>(chair);
      DestroyComponents<Piece>(chair);
      DestroyComponents<Chair>(chair);
      DestroyComponents<WearNTear>(chair);

      PrefabManager.Instance.AddPrefab(ghost);
    }

    private static string GetPrefabFriendlyName(GameObject prefab)
    {
      HoverText hover = prefab.GetComponent<HoverText>();
      if (hover) return hover.m_text;

      ItemDrop item = prefab.GetComponent<ItemDrop>();
      if (item) return item.m_itemData.m_shared.m_name;

      Character chara = prefab.GetComponent<Character>();
      if (chara) return chara.m_name;

      RuneStone runestone = prefab.GetComponent<RuneStone>();
      if (runestone) return runestone.m_name;

      ItemStand itemStand = prefab.GetComponent<ItemStand>();
      if (itemStand) return itemStand.m_name;

      MineRock mineRock = prefab.GetComponent<MineRock>();
      if (mineRock) return mineRock.m_name;

      Pickable pickable = prefab.GetComponent<Pickable>();
      if (pickable) return GetPrefabFriendlyName(pickable.m_itemPrefab);

      CreatureSpawner creatureSpawner = prefab.GetComponent<CreatureSpawner>();
      if (creatureSpawner) return GetPrefabFriendlyName(creatureSpawner.m_creaturePrefab);

      SpawnArea spawnArea = prefab.GetComponent<SpawnArea>();
      if (spawnArea && spawnArea.m_prefabs.Count > 0) {
        return GetPrefabFriendlyName(spawnArea.m_prefabs[0].m_prefab);
      }

      Piece piece = prefab.GetComponent<Piece>();
      if (piece && !string.IsNullOrEmpty(piece.m_name)) return piece.m_name;

      return prefab.name;
    }

    private static void CreatePrefabPiece(GameObject prefab)
    {
      InitPieceData(prefab);

      var pieceConfig = new PieceConfig
      {
        Name = prefab.name,
        Description = GetPrefabFriendlyName(prefab),
        PieceTable = "_HammerPieceTable",
        Category = GetPrefabCategory(prefab),
        AllowedInDungeons = true,
        Icon = CreatePrefabIcon(prefab)
      };

      var piece = new CustomPiece(prefab, true, pieceConfig);
      PieceManager.Instance.AddPiece(piece);
    }

    // Refs:
    //  - ZNetScene.m_prefabs
    public static void RegisterPrefabs(ZNetScene scene)
    {
      foreach (GameObject prefab in scene.m_prefabs)
      {
        if (ShouldIgnorePrefab(prefab)) continue;
        CreatePrefabPiece(prefab);
        CreateGhostPrefab(prefab);
      }
    }

    // Refs:
    //  - ZNetScene.m_prefabs
    //  - Piece
    public static void ModifyExistingPieces(ZNetScene scene)
    {
      foreach (GameObject prefab in scene.m_prefabs)
      {
        var piece = prefab.GetComponent<Piece>();
        if (piece)
          ModifyPiece(piece, false);
      }
    }

  }
}
