import 'package:flutter/material.dart';
import '../../albums/presentation/album_pages.dart';
import '../../memories/presentation/memory_pages.dart';
import '../../memories/presentation/timeline_page.dart';

class AuthenticatedShell extends StatefulWidget {
  const AuthenticatedShell({super.key});
  @override
  State<AuthenticatedShell> createState() => _AuthenticatedShellState();
}

class _AuthenticatedShellState extends State<AuthenticatedShell> {
  var _index = 0;
  final _visited = <int>{0};
  @override
  Widget build(BuildContext context) => Scaffold(
    body: IndexedStack(
      index: _index,
      children: [
        const TimelinePage(),
        _visited.contains(1) ? const AlbumsPage() : const SizedBox.shrink(),
        _visited.contains(2)
            ? const _SettingsPlaceholder()
            : const SizedBox.shrink(),
      ],
    ),
    floatingActionButton: FloatingActionButton(
      onPressed: () => Navigator.of(
        context,
      ).push(MaterialPageRoute<void>(builder: (_) => const CreateMemoryPage())),
      tooltip: 'Tạo kỷ niệm',
      child: const Icon(Icons.add),
    ),
    floatingActionButtonLocation: FloatingActionButtonLocation.centerDocked,
    bottomNavigationBar: NavigationBar(
      selectedIndex: _index,
      onDestinationSelected: (value) => setState(() {
        _visited.add(value);
        _index = value;
      }),
      destinations: const [
        NavigationDestination(
          icon: Icon(Icons.timeline_outlined),
          selectedIcon: Icon(Icons.timeline),
          label: 'Timeline',
        ),
        NavigationDestination(
          icon: Icon(Icons.photo_album_outlined),
          selectedIcon: Icon(Icons.photo_album),
          label: 'Album',
        ),
        NavigationDestination(
          icon: Icon(Icons.settings_outlined),
          selectedIcon: Icon(Icons.settings),
          label: 'Cài đặt',
        ),
      ],
    ),
  );
}

class _SettingsPlaceholder extends StatelessWidget {
  const _SettingsPlaceholder();
  @override
  Widget build(BuildContext context) => const Scaffold(
    body: Center(
      child: Padding(
        padding: EdgeInsets.all(24),
        child: Text(
          'Cài đặt sẽ được hoàn thiện trong Phase 19G.',
          textAlign: TextAlign.center,
        ),
      ),
    ),
  );
}
