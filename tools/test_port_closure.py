#!/usr/bin/env python3
"""Checks for the royal-throne port closure tools (plan Task 1.1).

Runs against git objects only (origin/royal-throne, origin/main); no Unity.

    python3 tools/test_port_closure.py
"""
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import port_asset_closure as pac  # noqa: E402
import port_code_closure as pcc  # noqa: E402

JS = pcc.JS
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


class Closure(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.files, cls.index = pcc.build(pcc.REF)
        cls.code = pcc.compute(cls.files, cls.index)
        cls.runtime = set(cls.code["runtime"])
        cls.tests = set(cls.code["tests"])
        cls.repo = pac.Repo(pcc.REF)
        cls.assets = pac.run(cls.repo, sorted(cls.runtime | cls.tests), cls.runtime | cls.tests, pac.DEFAULT_CUTS)
        cls.inc = set(cls.assets["included"])

    # --- C# closure -------------------------------------------------------
    def test_in_level_gui_subset_is_pulled_in(self):
        for name in ("RoyalGameplayPresentation", "RoyalGameplayHud", "RoyalBoardSurface", "RoyalArchShape",
                     "RoyalCardShape", "RoyalPanelGraphic", "RoyalVisualCatalog", "RoyalPressTracker", "RoyalTextStyle"):
            self.assertIn(JS + "GUI/%s.cs" % name, self.runtime)

    def test_excluded_areas_are_never_followed(self):
        for p in self.runtime | self.tests:
            self.assertIsNone(pcc.cut_label(p, pcc.DEFAULT_CUTS), p)
        for p in (JS + "Juice/RoyalWinSequence.cs", JS + "Juice/RoyalEconomy.cs", JS + "RoyalReferenceCampaign.cs",
                  JS + "GUI/RoyalHomeScreen.cs", "Assets/Characters/KingVenguard/V2/Scripts/KingVanguardSlot.cs"):
            self.assertNotIn(p, self.runtime | self.tests)

    def test_cuts_are_reported_with_lines(self):
        cuts = {(c["from"], c["type"]) for c in self.code["runtime_cuts"]}
        self.assertIn((JS + "LevelManager.cs", "RoyalWinSequence"), cuts)
        self.assertIn((JS + "GUI/RoyalGameplayPresentation.cs", "KingVanguardSlot"), cuts)
        self.assertTrue(all(c["lines"] for c in self.code["runtime_cuts"]))

    def test_partial_parts_in_old_boosters_are_not_dependencies(self):
        self.assertIn("Assets/Boosters/Propeller/V4/Scripts/PropellerGameplayV4.cs", self.code["partial_parts_cut"])
        self.assertNotIn("Assets/Boosters/Propeller/V4/Scripts/PropellerGameplayV4.cs", self.runtime)

    def test_spine_is_needed_only_through_the_cannon(self):
        spine = {p for p in self.runtime if p.startswith("Assets/Spine/")}
        self.assertTrue(spine)
        users = {p for p in self.runtime if not p.startswith("Assets/Spine/")
                 and any(t.startswith("Assets/Spine/") for t in pcc.resolve_refs(self.files[p], self.files, self.index))}
        self.assertEqual(users, {"Assets/ReferenceGameplay/Tools/Scripts/ReferenceCannonSequence.cs"})

    def test_plan_suites_compile_closure(self):
        for name in ("RoyalEffectsChecks", "RoyalOverlapChecks", "RoyalTNTChecks", "RoyalVfxLab"):
            self.assertIn(JS + "Editor/%s.cs" % name, self.tests)

    # --- asset closure ----------------------------------------------------
    def test_booster_prefabs_live_under_prefabs_resources(self):
        for p in ("Assets/Boosters/Propeller/V5/Prefabs/Resources/Propeller-V5.prefab",
                  "Assets/Boosters/ColorBall/V4/Prefabs/Resources/ColorBall-V4.prefab",
                  "Assets/Boosters/ColorBall/V5/Prefabs/Resources/ColorBall-V5.prefab"):
            self.assertIn(p, self.inc)

    def test_royal_tnt_is_not_in_the_closure_but_is_reported(self):
        self.assertFalse(any("/Resources/RoyalTNT/" in p for p in self.inc))
        self.assertTrue(any("/Resources/RoyalTNT/" in c["to"] and c["area"] == "TNT 3D fallback"
                            for c in self.assets["cuts"]))

    def test_cut_resources_stay_out(self):
        for p in self.inc:
            self.assertNotRegex(p, r"/RoyalGameplay/(Music|Win)/|RoyalCampaignArt/|/Characters/|castle-theme|royal-fanfare")

    def test_allin1_demo_assets_come_through_the_toolkit_library(self):
        self.assertIn("Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Demo/Prefabs/Fire Impact.prefab", self.inc)
        self.assertIn("Assets/JellyGarden/Resources/RoyalGameplay/Toolkit/Library.asset", self.inc)

    def test_webp_importer_found_through_meta(self):
        self.assertIn(JS + "Editor/RoyalWebPImporter.cs", self.inc)

    def test_prefix_load_keeps_its_slash(self):
        # "RoyalGameplay/" + name must not match RoyalGameplayV4/...
        rg = [n for n in self.assets["narrowed"] if n[1] == "RoyalGameplay/"]
        self.assertTrue(rg)
        for _, _, _, _, dropped in rg:
            self.assertFalse(any(k.startswith("RoyalGameplayV4") for k in dropped))

    def test_narrowing_keeps_the_layout_keys_code_names(self):
        self.assertIn("Assets/ReferenceGameplay/Layout/Resources/ReferenceGameplay/Layout/Game-settings_button_gear.png",
                      self.inc)


class ImportPaths(unittest.TestCase):
    def test_file_is_well_formed_and_resolvable(self):
        path = os.path.join(ROOT, "docs/port/import-paths.txt")
        if not os.path.exists(path):
            self.skipTest("run tools/port_import_manifest.py first")
        repo = pac.Repo(pcc.REF)
        groups = []
        with open(path) as fh:
            lines = fh.read().splitlines()
        for line in lines:
            if not line:
                continue
            if line.startswith("#"):
                if line[2:] in ("code", "editor-tests", "third-party", "assets"):
                    groups.append(line[2:])
                continue
            self.assertFalse(" (" in line, "prose on a path line: " + line)
            if line.endswith("/"):
                self.assertTrue(any(p.startswith(line) for p in repo.files), line)
            else:
                self.assertIn(line, repo.files)
                self.assertIn(line + ".meta", repo.files)
            self.assertNotRegex(line, r"RoyalTNT/|/Music/|/Win/|RoyalCampaign|Characters/|EffectsProfile\.asset")
        self.assertEqual(groups, ["code", "editor-tests", "third-party", "assets"])


if __name__ == "__main__":
    unittest.main(verbosity=2)
