#!/usr/bin/env python3
"""Emit a globbed csproj so `dotnet build` typechecks newly added scripts.

Unity regenerates Assembly-CSharp*.csproj with an explicit <Compile Include>
per file, which means a file added outside the editor is silently not
typechecked. This derives a sibling project from Assembly-CSharp-Editor.csproj
(the one that already references UnityEditor) and swaps the explicit file list
for a glob over every script under Assets/. Compiling runtime and editor code
into one assembly is wrong for a build but exactly right for a typecheck.

    python3 tools/make_typecheck_csproj.py && dotnet build Story.Typecheck.csproj
"""

import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import PROJECT_ROOT  # noqa: E402

SOURCE = os.path.join(PROJECT_ROOT, "Assembly-CSharp-Editor.csproj")
DEST = os.path.join(PROJECT_ROOT, "Story.Typecheck.csproj")

GLOB = (
    '  <ItemGroup>\n'
    '    <Compile Include="Assets/**/*.cs" />\n'
    '  </ItemGroup>\n'
)


def main():
    with open(SOURCE, encoding="utf-8-sig") as fh:
        text = fh.read()

    # Drop every explicit compile item and the (now empty) group that held them.
    text = re.sub(r'[ \t]*<Compile Include="[^"]*" />\r?\n', "", text)
    text = re.sub(r'[ \t]*<ItemGroup>\s*</ItemGroup>\r?\n', "", text)

    # A ProjectReference back to Assembly-CSharp would double-define every type,
    # because the glob already pulls those files in here.
    text = re.sub(
        r'[ \t]*<ProjectReference Include="[^"]*Assembly-CSharp\.csproj">.*?</ProjectReference>\r?\n',
        "",
        text,
        flags=re.DOTALL,
    )
    text = re.sub(
        r'[ \t]*<ProjectReference Include="[^"]*Assembly-CSharp\.csproj" />\r?\n', "", text
    )

    text = text.replace("<AssemblyName>Assembly-CSharp-Editor</AssemblyName>",
                        "<AssemblyName>Story.Typecheck</AssemblyName>")
    text = text.replace("<OutputPath>Temp/bin/Debug/</OutputPath>",
                        "<OutputPath>Temp/bin/Typecheck/</OutputPath>")
    text = text.replace("<BaseIntermediateOutputPath>Temp/obj/$(MSBuildProjectName)</BaseIntermediateOutputPath>",
                        "<BaseIntermediateOutputPath>Temp/obj/StoryTypecheck</BaseIntermediateOutputPath>")

    # Reinsert the glob just before the closing tag.
    marker = "</Project>"
    text = text[: text.rindex(marker)] + GLOB + marker + "\n"

    with open(DEST, "w", encoding="utf-8") as fh:
        fh.write(text)

    print("wrote %s" % os.path.relpath(DEST, PROJECT_ROOT))
    return 0


if __name__ == "__main__":
    sys.exit(main())
