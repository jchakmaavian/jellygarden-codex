#!/usr/bin/env python3
"""Self-test for port_guid_audit.py on a scratch git repository.

Builds a throwaway repo with a kit commit and two branches ("main" and "rt")
that exercise every list the audit reports, then checks the classification.
It never touches this project's repository.

    python3 tools/test_port_guid_audit.py
"""

import contextlib
import io
import os
import shutil
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import port_guid_audit as audit  # noqa: E402

PNG = b"\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR" + bytes(range(64))


def meta(guid, importer="DefaultImporter", body=""):
    return "fileFormatVersion: 2\nguid: %s\n%s:\n%s  userData: \n" % (guid, importer, body)


def texture_meta(guid, size, android=False):
    body = ("  serializedVersion: 12\n  textureType: 8\n  spriteMode: 1\n  platformSettings:\n"
            "  - serializedVersion: 3\n    buildTarget: DefaultTexturePlatform\n    maxTextureSize: %d\n"
            "    textureCompression: 1\n    overridden: 0\n" % size)
    if android:
        body += ("  - serializedVersion: 3\n    buildTarget: Android\n    maxTextureSize: %d\n"
                 "    textureFormat: 48\n    overridden: 1\n" % size)
    body += "  spriteSheet:\n    serializedVersion: 2\n    sprites: []\n"
    return meta(guid, "TextureImporter", body)


G = {k: k * 32 for k in "abcdef"}
G.update({"g": "0a" * 16, "h": "0b" * 16,"m": "1" * 32, "p1": "2" * 32, "p2": "3" * 32, "dup": "4" * 32, "tex": "5" * 32, "eol": "6" * 32,
          "same": "7" * 32, "code": "8" * 32, "user": "9" * 32})


class AuditTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.tmp = tempfile.mkdtemp(prefix="guid-audit-test-")
        cls.repo = os.path.join(cls.tmp, "repo")
        os.makedirs(cls.repo)
        cls.git("init", "-q", "-b", "kit")

        cls.write({
            "Assets/Plugins.meta": meta(G["p1"]),
            "Assets/Plugins/keep.txt": "plugin\n",
            "Assets/Plugins/keep.txt.meta": meta(G["h"]),
            "Assets/Folder.meta": meta(G["f"]),
            "Assets/Folder/A.png": PNG,
            "Assets/Folder/A.png.meta": texture_meta(G["a"], 1024),
            "Assets/B.txt": "kit level\n",
            "Assets/B.txt.meta": meta(G["b"], "TextScriptImporter"),
            "Assets/Code.cs": "public class Code {\n    public void Foo() {}\n}\n",
            "Assets/Code.cs.meta": meta(G["code"], "MonoImporter"),
            "Assets/Moved.prefab": "%YAML 1.1\n--- !u!1 &1\nGameObject:\n  m_Name: Moved\n",
            "Assets/Moved.prefab.meta": meta(G["m"], "PrefabImporter"),
            "Assets/Same.mat": "%YAML 1.1\nMaterial:\n  m_Name: Same\n",
            "Assets/Same.mat.meta": meta(G["same"], "NativeFormatImporter"),
        })
        cls.commit("kit")

        cls.git("checkout", "-q", "-b", "main")
        cls.write({
            "Assets/B.txt": "main level, edited\n",
            "Assets/Folder/A.png.meta": texture_meta(G["a"], 512),
            "Assets/Eol.cs": "public class Eol {\n    int x;\n}\n",
            "Assets/Eol.cs.meta": meta(G["eol"], "MonoImporter"),
            "Assets/Tex.png": PNG + b"main",
            "Assets/Tex.png.meta": texture_meta(G["tex"], 1024),
        })
        cls.commit("main")

        cls.git("checkout", "-q", "kit")
        cls.git("checkout", "-q", "-b", "rt")
        cls.git("rm", "-q", "Assets/Moved.prefab", "Assets/Moved.prefab.meta")
        cls.write({
            "Assets/Plugins.meta": meta(G["p2"]),
            "Assets/Folder/A.png.meta": texture_meta(G["a"], 1024, android=True),
            "Assets/Code.cs": "public class Code {\n    public void Foo() {}\n    public void Bar() {}\n}\n",
            "Assets/Other.meta": meta(G["g"]),
            "Assets/Other/Moved.prefab": "%YAML 1.1\n--- !u!1 &1\nGameObject:\n  m_Name: Moved\n",
            "Assets/Other/Moved.prefab.meta": meta(G["m"], "PrefabImporter"),
            "Assets/Eol.cs": "public class Eol {\r\n    int x;\r\n}\r\n",
            "Assets/Eol.cs.meta": meta(G["eol"], "MonoImporter"),
            "Assets/Tex.png": PNG + b"royal",
            "Assets/Tex.png.meta": texture_meta(G["tex"], 1024),
            "Assets/Juice.meta": meta(G["c"]),
            "Assets/Juice/User.cs": "public class User {\n    void Go(Code c) { c.Bar(); }\n}\n",
            "Assets/Juice/User.cs.meta": meta(G["user"], "MonoImporter"),
            "Assets/Juice/Ref.prefab": "%%YAML 1.1\nSpriteRenderer:\n  m_Sprite: {fileID: 21300000, guid: %s, type: 3}\n"
                                       % G["tex"],
            "Assets/Juice/Ref.prefab.meta": meta(G["d"], "PrefabImporter"),
            "Assets/Dup1.asset": "%YAML 1.1\nMonoBehaviour:\n  m_Name: Dup1\n",
            "Assets/Dup1.asset.meta": meta(G["dup"], "NativeFormatImporter"),
            "Assets/Dup2.asset": "%YAML 1.1\nMonoBehaviour:\n  m_Name: Dup2\n",
            "Assets/Dup2.asset.meta": meta(G["dup"], "NativeFormatImporter"),
        })
        cls.commit("rt")
        cls.old_repo = audit.REPO
        audit.REPO = cls.repo

    @classmethod
    def tearDownClass(cls):
        audit.REPO = cls.old_repo
        shutil.rmtree(cls.tmp, ignore_errors=True)

    @classmethod
    def git(cls, *args):
        subprocess.run(["git", "-C", cls.repo, "-c", "user.name=t", "-c", "user.email=t@t", "-c", "core.autocrlf=false",
                        *args], check=True, capture_output=True)

    @classmethod
    def write(cls, files):
        for rel, content in files.items():
            path = os.path.join(cls.repo, rel)
            os.makedirs(os.path.dirname(path), exist_ok=True)
            with open(path, "wb") as fh:
                fh.write(content if isinstance(content, bytes) else content.encode())
        cls.git("add", "-A")

    @classmethod
    def commit(cls, msg):
        cls.git("commit", "-q", "-m", msg)

    def run_audit(self, prefixes=None):
        main, rt, rt_sel, res = audit.audit("main", "rt", prefixes)
        kit = audit.Side("kit")
        audit.enrich(main, rt, kit, res, prefixes)
        report = audit.render(main, rt, kit, rt_sel, res, prefixes)
        return res, report

    def b(self, res, path):
        found = [e for e in res["b"] if e["path"] == path]
        self.assertEqual(len(found), 1, path)
        return found[0]

    def test_lists(self):
        res, report = self.run_audit()
        self.assertIn("Assets/Same.mat", [e["path"] for e in res["a"]])
        self.assertIn("Assets/Plugins/keep.txt", [e["path"] for e in res["a"]])

        level = self.b(res, "Assets/B.txt")
        self.assertEqual(level["class"], "content differs")
        self.assertEqual(level["side"], "main")            # royal-throne still has the kit's bytes
        self.assertEqual(level["changed_by"], "asset: main")

        tex = self.b(res, "Assets/Folder/A.png")
        self.assertEqual(tex["class"], "texture/meta reimport only")
        self.assertEqual(tex["side"], "main")
        self.assertTrue(any(d.startswith("maxTextureSize 512 -> 1024") for d in tex["meta_settings"]))
        self.assertTrue(any(d.startswith("androidOverrides") for d in tex["meta_settings"]))

        code = self.b(res, "Assets/Code.cs")
        self.assertEqual(code["side"], "royal-throne")      # royal-throne code calls the member it added
        self.assertEqual(list(code["rt_added_used"]), ["Bar"])

        eol = self.b(res, "Assets/Eol.cs")
        self.assertEqual(eol["class"], "content differs (line endings only)")
        self.assertEqual(eol["side"], "main")

        shared = self.b(res, "Assets/Tex.png")
        self.assertEqual(shared["changed_by"], "not in kit")
        self.assertEqual(shared["side"], "royal-throne")    # referenced by a royal-throne-only prefab
        self.assertEqual(shared["rt_live_users"], ["Assets/Juice/Ref.prefab"])

        self.assertEqual([e["guid"] for e in res["c"]], [G["m"]])
        self.assertEqual(audit.c_kind(res["c"][0]), "moved (same bytes)")
        self.assertEqual([e["paths"] for e in res["c_rt_dup"]], [["Assets/Dup1.asset", "Assets/Dup2.asset"]])
        self.assertEqual([(e["path"], e["main_guid"], e["rt_guid"]) for e in res["d"]],
                         [("Assets/Plugins", G["p1"], G["p2"])])
        self.assertIn("Assets/Moved.prefab", res["main_only_in_rt_dirs"])
        self.assertIn("Assets/Juice/User.cs", res["rt_only"])

        for heading in ("## Counts", "## Key findings", "## (a) Identical", "## (b) Same path and GUID",
                        "## (c) Same GUID at different paths", "## (d) Same path, different GUID"):
            self.assertIn(heading, report)

    def test_paths_file_restricts_royal_throne_side(self):
        res, report = self.run_audit(["Assets/Juice"])
        self.assertEqual(res["b"], [])
        self.assertEqual(res["c"], [])
        self.assertEqual(res["d"], [])
        self.assertEqual(res["c_rt_dup"], [])
        self.assertIn("Assets/Juice/Ref.prefab", res["rt_only"])

    def test_cli_exit_code_and_working_tree_untouched(self):
        status_before = subprocess.run(["git", "-C", self.repo, "status", "--porcelain"], capture_output=True).stdout
        out = io.StringIO()
        with contextlib.redirect_stdout(out), contextlib.redirect_stderr(io.StringIO()):
            code = audit.main_cli(["--repo", self.repo, "--main-ref", "main", "--rt-ref", "rt", "--kit-ref", "kit"])
        self.assertEqual(code, 1)                           # (c) and (d) are not empty
        self.assertIn("# GUID audit: main vs rt", out.getvalue())
        status_after = subprocess.run(["git", "-C", self.repo, "status", "--porcelain"], capture_output=True).stdout
        self.assertEqual(status_before, status_after)

        paths = os.path.join(self.tmp, "paths.txt")
        with open(paths, "w") as fh:
            fh.write("# only the level layer\nAssets/Juice\n")
        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            code = audit.main_cli(["--repo", self.repo, "--main-ref", "main", "--rt-ref", "rt", "--kit-ref", "kit",
                                   "--paths-file", paths])
        self.assertEqual(code, 0)

    def test_meta_parser(self):
        old = (b"fileFormatVersion: 2\nguid: 0123456789abcdef0123456789abcdef\nTextureImporter:\n"
               b"  fileIDToRecycleName:\n    21300000: star\n  maxTextureSize: 512\n  textureType: 5\n"
               b"  textureSettings:\n    filterMode: -1\n    wrapMode: -1\n  spritePivot: {x: .5, y: .5}\n")
        new = (b"fileFormatVersion: 2\nguid: 0123456789abcdef0123456789abcdef\nTextureImporter:\n"
               b"  internalIDToNameTable:\n  - first:\n      213: 21300000\n    second: star\n"
               b"  serializedVersion: 13\n  textureType: 0\n  textureSettings:\n    serializedVersion: 2\n"
               b"    filterMode: 1\n    wrapU: 0\n  spritePivot: {x: 0.5, y: 0.5}\n  platformSettings:\n"
               b"  - serializedVersion: 4\n    buildTarget: DefaultTexturePlatform\n    maxTextureSize: 512\n")
        reser, change, diffs = audit.compare_metas(old, new)
        self.assertTrue(reser)
        self.assertIsNone(change)
        self.assertEqual(diffs, [])                         # format upgrade only: nothing that matters changed
        self.assertEqual(audit.sub_asset_ids(old), audit.sub_asset_ids(new))
        self.assertEqual(audit.sub_asset_ids(old), {"star": "21300000"})


if __name__ == "__main__":
    unittest.main(verbosity=2)
