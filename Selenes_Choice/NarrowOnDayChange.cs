﻿using HarmonyLib;
using LethalLevelLoader;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Selenes_Choice
{
    [HarmonyPatch(typeof(StartOfRound), "PassTimeToNextDay")]
    public class HideMoonsOnDayChange
    {
        public static int glump;
        static void Postfix()
        {
            ShareSnT.Instance.StartCoroutine(WaitOnDay());
        }
        static IEnumerator WaitOnDay()
        {
            yield return new WaitForSeconds(2);
            ProcessData();
        }
        static void ProcessData()
        {
            Selenes_Choice.LastUsedSeed = StartOfRound.Instance.randomMapSeed;
            ES3.Save<int>("LastUsedSeed", Selenes_Choice.LastUsedSeed, GameNetworkManager.Instance.currentSaveFileName);

            List<ExtendedLevel> allLevels = PatchedContent.ExtendedLevels.Where(level => !ListProcessor.Instance.ExclusionList.Split(',').Any(b => level.NumberlessPlanetName.Equals(b))).ToList();

            foreach (ExtendedLevel level in allLevels)
            {
                level.IsRouteLocked = false;
                level.IsRouteHidden = true;
            }
            List<ExtendedLevel> freeLevels = allLevels.Where(level => level.RoutePrice == 0).ToList();
            List<ExtendedLevel> paidLevels = allLevels.Where(level => level.RoutePrice != 0).ToList();

            ExtendedLevel randomFreeLevel = null;

            Selenes_Choice.instance.mls.LogInfo("Start Seed " + StartOfRound.Instance.randomMapSeed); // uses the previous map seed
            Random.State originalState = Random.state;
            Random.InitState(StartOfRound.Instance.randomMapSeed);

            PriceManager.ResetPrices();
            UpdateConfig.Instance.BracketMoons();

            int randomFreeIndex = Random.Range(0, freeLevels.Count); // gets the one holy "safety moon"
            randomFreeLevel = freeLevels[randomFreeIndex];
            randomFreeLevel.IsRouteHidden = false;
            if (Selenes_Choice.Config.ClearWeather)
            {
                randomFreeLevel.SelectableLevel.currentWeather = LevelWeatherType.None;
            }
            allLevels.Remove(randomFreeLevel);
            freeLevels.Remove(randomFreeLevel);
            Selenes_Choice.PreviousSafetyMoon = randomFreeLevel;

            Selenes_Choice.instance.mls.LogInfo("Safety Moon: " + randomFreeLevel.SelectableLevel.PlanetName);

            for (int i = 0; i < (UpdateConfig.freeMoonCount - 1); i++) // gets other free moons
            {
                int randomExtraFreeIndex = Random.Range(0, freeLevels.Count);
                ExtendedLevel additionalFreeLevels = freeLevels[randomExtraFreeIndex];
                additionalFreeLevels.IsRouteHidden = false;
                freeLevels.Remove(additionalFreeLevels);
                allLevels.Remove(additionalFreeLevels);
            }

            if (UpdateConfig.paidMoonCount != 0)
            {
                for (int i = 0; i < UpdateConfig.paidMoonCount; i++) // gets some paid moons
                {
                    int PaidIndex = Random.Range(0, paidLevels.Count);
                    ExtendedLevel PaidLevel = paidLevels[PaidIndex];
                    PaidLevel.IsRouteHidden = false;
                    paidLevels.Remove(PaidLevel);
                    allLevels.Remove(PaidLevel);
                    if (Selenes_Choice.Config.DiscountMoons)
                    {
                        PriceManager.originalPrices[PaidLevel] = PaidLevel.RoutePrice;
                        int seed = StartOfRound.Instance.randomMapSeed + glump;
                        System.Random rand = new System.Random(seed);
                        int randomNumber = rand.Next(UpdateConfig.minDiscount, UpdateConfig.maxDiscount + 1);
                        int newRandomNumber = 100 - randomNumber;
                        float discountValue = newRandomNumber / 100f;
                        PaidLevel.RoutePrice = (int)(PaidLevel.RoutePrice * discountValue);
                        glump++;
                    }
                }
            }

            for (int i = 0; i < UpdateConfig.randomMoonCount; i++) // gets any other additional moons
            {
                int randomIndex = Random.Range(0, allLevels.Count);
                ExtendedLevel randomLevel = allLevels[randomIndex];
                randomLevel.IsRouteHidden = false;
                allLevels.Remove(randomLevel);
                if (Selenes_Choice.Config.DiscountMoons)
                {
                    PriceManager.originalPrices[randomLevel] = randomLevel.RoutePrice;
                    int seed = StartOfRound.Instance.randomMapSeed + glump;
                    System.Random rand = new System.Random(seed);
                    int randomNumber = rand.Next(UpdateConfig.minDiscount, UpdateConfig.maxDiscount + 1);
                    int newRandomNumber = 100 - randomNumber;
                    float discountValue = newRandomNumber / 100f;
                    randomLevel.RoutePrice = (int)(randomLevel.RoutePrice * discountValue);
                    glump++;
                }
            }
            glump = 0;
            Random.state = originalState;

            foreach (ExtendedLevel level in allLevels)
            {
                if (level.IsRouteHidden)
                {
                    level.IsRouteLocked = true;
                }
            }

            if (UpdateConfig.autoRoute) {
                if (TimeOfDay.Instance.daysUntilDeadline == 0)
                {
                    ExtendedLevel gordionLevel = PatchedContent.ExtendedLevels.FirstOrDefault(level => level.NumberlessPlanetName.Equals("Gordion"));

                    int CompanyID = gordionLevel.SelectableLevel.levelID;

                    if (gordionLevel != LevelManager.CurrentExtendedLevel)
                    {
                        StartOfRound.Instance.ChangeLevelServerRpc(CompanyID, Object.FindObjectOfType<Terminal>().groupCredits);
                    }
                }
                else
                {
                    if (randomFreeLevel != null && randomFreeLevel != LevelManager.CurrentExtendedLevel)
                    {
                        int randomFreeLevelId = randomFreeLevel.SelectableLevel.levelID;

                        StartOfRound.Instance.ChangeLevelServerRpc(randomFreeLevelId, Object.FindObjectOfType<Terminal>().groupCredits);
                    }
                }
            }

            
        }
    }
    [HarmonyPatch(typeof(StartOfRound), "PassTimeToNextDay")]
    public class KeepWeather
    {
        static void Postfix()
        {
            ShareSnT.Instance.StartCoroutine(WaitOnWeather());
        }
        static IEnumerator WaitOnWeather()
        {
            yield return new WaitForSeconds(2);
            SetTheWeather();
        }
        static void SetTheWeather()
        {
            Selenes_Choice.PreviousSafetyMoon.SelectableLevel.currentWeather = LevelWeatherType.None;
            StartOfRound.Instance.SetMapScreenInfoToCurrentLevel();
        }
    }
}
