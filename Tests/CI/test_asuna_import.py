import importlib.util
import io
from pathlib import Path
import tarfile
import tempfile
import unittest

MODULE = Path(__file__).resolve().parents[2] / 'Tools/Art/import_asuna.py'
SPEC = importlib.util.spec_from_file_location('import_asuna', MODULE)
IMPORTER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(IMPORTER)


class AsunaImportTests(unittest.TestCase):
    def package(self, folder, entries):
        package = folder / 'character.unitypackage'
        with tarfile.open(package, 'w:gz') as archive:
            for index, (path, content) in enumerate(entries):
                for name, data in [('pathname', path.encode()), ('asset', content), ('asset.meta', b'guid: preserved')]:
                    member = tarfile.TarInfo(f'{index}/{name}')
                    member.size = len(data)
                    archive.addfile(member, io.BytesIO(data))
        return package

    def test_imports_model_and_guid_but_excludes_scripts_and_archives(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            package = self.package(root, [('Assets/FreeTestCharacterAsuna/model.fbx', b'model'),
                                          ('Assets/FreeTestCharacterAsuna/tool.cs', b'code'),
                                          ('Assets/FreeTestCharacterAsuna/source.zip', b'archive')])
            IMPORTER.import_package(package, root)
            self.assertEqual((root / 'Assets/FreeTestCharacterAsuna/model.fbx').read_bytes(), b'model')
            self.assertEqual((root / 'Assets/FreeTestCharacterAsuna/model.fbx.meta').read_bytes(), b'guid: preserved')
            self.assertFalse((root / 'Assets/FreeTestCharacterAsuna/tool.cs').exists())
            self.assertFalse((root / 'Assets/FreeTestCharacterAsuna/source.zip').exists())

    def test_rejects_traversal_and_unrelated_project_paths(self):
        for path in ['../outside.fbx', 'Assets/Elsewhere/model.fbx', 'Assets\\FreeTestCharacterAsuna\\model.fbx']:
            with self.subTest(path=path), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                with self.assertRaises(ValueError):
                    IMPORTER.import_package(self.package(root, [(path, b'model')]), root)
                self.assertFalse((root / 'Assets').exists())
