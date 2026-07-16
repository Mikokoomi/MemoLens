class AlbumListItem {
  const AlbumListItem({
    required this.id,
    required this.title,
    this.description,
    required this.memoryCount,
    this.effectiveCoverImageId,
    required this.createdAt,
  });
  final int id;
  final String title;
  final String? description;
  final int memoryCount;
  final int? effectiveCoverImageId;
  final DateTime createdAt;
  factory AlbumListItem.fromJson(Map<String, dynamic> json) => AlbumListItem(
    id: json['id'] as int,
    title: json['title'] as String,
    description: json['description'] as String?,
    memoryCount: json['memoryCount'] as int? ?? 0,
    effectiveCoverImageId: json['effectiveCoverImageId'] as int?,
    createdAt: DateTime.parse(json['createdAt'] as String),
  );
}

class AlbumMemoryItem {
  const AlbumMemoryItem({
    required this.id,
    required this.title,
    required this.memoryDate,
    required this.feeling,
    required this.imageCount,
    this.location,
    this.coverImageId,
  });
  final int id;
  final String title;
  final DateTime memoryDate;
  final String feeling;
  final int imageCount;
  final String? location;
  final int? coverImageId;
  factory AlbumMemoryItem.fromJson(Map<String, dynamic> json) =>
      AlbumMemoryItem(
        id: json['id'] as int,
        title: json['title'] as String,
        memoryDate: DateTime.parse(json['memoryDate'] as String),
        feeling: json['feeling'] as String? ?? '',
        imageCount: json['imageCount'] as int? ?? 0,
        location: json['location'] as String?,
        coverImageId: json['effectiveCoverImageId'] as int?,
      );
}

class AlbumDetails extends AlbumListItem {
  const AlbumDetails({
    required super.id,
    required super.title,
    super.description,
    required super.memoryCount,
    super.effectiveCoverImageId,
    required super.createdAt,
    required this.updatedAt,
    required this.memories,
  });
  final List<AlbumMemoryItem> memories;
  final DateTime updatedAt;
  factory AlbumDetails.fromJson(Map<String, dynamic> json) => AlbumDetails(
    id: json['id'] as int,
    title: json['title'] as String,
    description: json['description'] as String?,
    memoryCount: json['memoryCount'] as int? ?? 0,
    effectiveCoverImageId: json['effectiveCoverImageId'] as int?,
    createdAt: DateTime.parse(json['createdAt'] as String),
    updatedAt: DateTime.parse(json['updatedAt'] as String),
    memories: ((json['memories'] as Map?)?['items'] as List? ?? const [])
        .map(
          (item) =>
              AlbumMemoryItem.fromJson(Map<String, dynamic>.from(item as Map)),
        )
        .toList(growable: false),
  );
}

class AlbumDraft {
  const AlbumDraft({
    required this.title,
    this.description,
    this.memoryIds = const [],
  });
  final String title;
  final String? description;
  final List<int> memoryIds;
  Map<String, dynamic> toJson({bool includeMemories = true}) => {
    'title': title.trim(),
    'description': description?.trim().isEmpty == true
        ? null
        : description?.trim(),
    if (includeMemories) 'memoryIds': memoryIds.toSet().toList(growable: false),
  };
}

class AlbumPage {
  const AlbumPage({required this.items});
  final List<AlbumListItem> items;
  factory AlbumPage.fromJson(Map<String, dynamic> json) => AlbumPage(
    items: (json['items'] as List? ?? const [])
        .map(
          (item) =>
              AlbumListItem.fromJson(Map<String, dynamic>.from(item as Map)),
        )
        .toList(growable: false),
  );
}
