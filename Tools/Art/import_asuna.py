"""Import the user's licensed Asuna package without embedded archives or scripts."""
import argparse
import json
from pathlib import Path, PurePosixPath
import shutil
import tarfile

ALLOWED = {'.fbx', '.png', '.tga', '.mat', '.prefab', '.controller', '.mask', '.anim', '.pdf'}


def import_package(package, project):
    project = Path(project).resolve()
    destination = project / 'Assets' / 'FreeTestCharacterAsuna'
    paths = {}
    with tarfile.open(package, 'r:gz') as archive:
        for member in archive:
            parts = member.name.split('/')
            if len(parts) == 2 and parts[1] == 'pathname' and member.isfile():
                paths[parts[0]] = archive.extractfile(member).read().decode('utf-8')
    imported = 0
    with tarfile.open(package, 'r:gz') as archive:
        for member in archive:
            parts = member.name.split('/')
            if len(parts) != 2 or parts[1] not in {'asset', 'asset.meta'} or not member.isfile():
                continue
            name = paths.get(parts[0], '')
            path = PurePosixPath(name)
            if not name or path.suffix.lower() not in ALLOWED:
                continue
            if '\\' in name or path.is_absolute() or '..' in path.parts:
                raise ValueError(f'Unsafe package path: {name}')
            output = (project / name).resolve()
            if not output.is_relative_to(destination):
                raise ValueError(f'Unexpected package path: {name}')
            if parts[1] == 'asset.meta':
                output = Path(str(output) + '.meta')
            output.parent.mkdir(parents=True, exist_ok=True)
            with output.open('wb') as stream:
                shutil.copyfileobj(archive.extractfile(member), stream)
            imported += 1
    print(json.dumps({'imported_files': imported, 'destination': str(destination)}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('package', type=Path)
    parser.add_argument('--project', type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    import_package(args.package, args.project)
