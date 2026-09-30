# Reference gameplay import source package

This branch contains the scoped import package and its validation evidence, not a standalone Unity project. Apply it to the existing JellyGarden-Codex working project checkpoint recorded in Design/ReferenceGameplayImport/checkpoint-path.txt. The original implementation commit is cdd68361; its parent/base is b3bb51d2c76a204dbd13cf06875093c2166e4811.

The package preserves Unity .meta identities and includes the imported gameplay assets, modified adapters, required inherited campaign prerequisites, editable import inputs, level-source evidence, test reports and the gameplay recording. Existing baseline Unity assets/packages/scenes are required; they are intentionally not duplicated here. See Design/ReferenceGameplayImport/INTEGRATION.md for runtime connections and incomplete native behavior.

This separate backup avoids uploading unrelated unpublished animation histories. The local working branch and earlier commits remain unchanged.
