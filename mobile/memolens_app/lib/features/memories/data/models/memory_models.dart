import 'package:intl/intl.dart';

const memoryFeelings = <String>[
  'Bình yên',
  'Vui vẻ',
  'Buồn',
  'Nhớ',
  'Lo lắng',
  'Mệt mỏi',
  'Khó chịu',
  'Lẫn lộn',
  'Khác',
];

String formatMemoryDate(DateTime date) => DateFormat('yyyy-MM-dd').format(date);
DateTime parseMemoryDate(Object? value) {
  final parsed = DateTime.parse(value.toString());
  return DateTime(parsed.year, parsed.month, parsed.day);
}

class MemoryImageMetadata {
  const MemoryImageMetadata({
    required this.id,
    required this.originalFileName,
    required this.uploadedAt,
    required this.contentUrl,
  });
  final int id;
  final String originalFileName;
  final DateTime uploadedAt;
  final String contentUrl;
  factory MemoryImageMetadata.fromJson(Map<String, dynamic> json) =>
      MemoryImageMetadata(
        id: json['id'] as int,
        originalFileName: json['originalFileName'] as String? ?? 'Ảnh kỷ niệm',
        uploadedAt: DateTime.parse(json['uploadedAt'] as String),
        contentUrl: json['contentUrl'] as String? ?? '',
      );
}

class MemoryListItem {
  const MemoryListItem({
    required this.id,
    required this.title,
    required this.shortStoryPreview,
    required this.feeling,
    required this.memoryDate,
    required this.location,
    required this.tags,
    required this.imageCount,
    required this.coverImageId,
    required this.createdAt,
    required this.updatedAt,
  });
  final int id;
  final String title;
  final String? shortStoryPreview;
  final String feeling;
  final DateTime memoryDate;
  final String? location;
  final List<String> tags;
  final int imageCount;
  final int? coverImageId;
  final DateTime createdAt;
  final DateTime updatedAt;
  factory MemoryListItem.fromJson(Map<String, dynamic> json) => MemoryListItem(
    id: json['id'] as int,
    title: json['title'] as String,
    shortStoryPreview: json['shortStoryPreview'] as String?,
    feeling: json['feeling'] as String,
    memoryDate: parseMemoryDate(json['memoryDate']),
    location: json['location'] as String?,
    tags: List<String>.from(json['tags'] as List? ?? const []),
    imageCount: json['imageCount'] as int? ?? 0,
    coverImageId: json['coverImageId'] as int?,
    createdAt: DateTime.parse(json['createdAt'] as String),
    updatedAt: DateTime.parse(json['updatedAt'] as String),
  );
  factory MemoryListItem.fromDetails(MemoryDetails details) => MemoryListItem(
    id: details.id,
    title: details.title,
    shortStoryPreview: details.story,
    feeling: details.feeling,
    memoryDate: details.memoryDate,
    location: details.location,
    tags: details.tags,
    imageCount: details.images.length,
    coverImageId: details.images.isEmpty ? null : details.images.first.id,
    createdAt: details.createdAt,
    updatedAt: details.updatedAt,
  );
}

class MemoryDetails {
  const MemoryDetails({
    required this.id,
    required this.title,
    required this.story,
    required this.feeling,
    required this.memoryDate,
    required this.location,
    required this.tags,
    required this.images,
    required this.createdAt,
    required this.updatedAt,
  });
  final int id;
  final String title;
  final String? story;
  final String feeling;
  final DateTime memoryDate;
  final String? location;
  final List<String> tags;
  final List<MemoryImageMetadata> images;
  final DateTime createdAt;
  final DateTime updatedAt;
  factory MemoryDetails.fromJson(Map<String, dynamic> json) => MemoryDetails(
    id: json['id'] as int,
    title: json['title'] as String,
    story: json['story'] as String?,
    feeling: json['feeling'] as String,
    memoryDate: parseMemoryDate(json['memoryDate']),
    location: json['location'] as String?,
    tags: List<String>.from(json['tags'] as List? ?? const []),
    images: (json['images'] as List? ?? const [])
        .map(
          (item) => MemoryImageMetadata.fromJson(
            Map<String, dynamic>.from(item as Map),
          ),
        )
        .toList(growable: false),
    createdAt: DateTime.parse(json['createdAt'] as String),
    updatedAt: DateTime.parse(json['updatedAt'] as String),
  );
}

enum MemorySort { newest, oldest }

class MemoryQuery {
  const MemoryQuery({
    this.page = 1,
    this.pageSize = 20,
    this.search,
    this.feeling,
    this.tag,
    this.from,
    this.to,
    this.year,
    this.month,
    this.sort = MemorySort.newest,
  });
  final int page;
  final int pageSize;
  final String? search;
  final String? feeling;
  final String? tag;
  final DateTime? from;
  final DateTime? to;
  final int? year;
  final int? month;
  final MemorySort sort;
  Map<String, dynamic> toQueryParameters() => {
    'page': page,
    'pageSize': pageSize,
    if (_hasText(search)) 'search': search!.trim(),
    if (_hasText(feeling)) 'feeling': feeling,
    if (_hasText(tag)) 'tag': tag!.trim(),
    if (from != null) 'from': formatMemoryDate(from!),
    if (to != null) 'to': formatMemoryDate(to!),
    if (year != null && from == null && to == null) 'year': year,
    if (month != null && from == null && to == null) 'month': month,
    'sort': sort.name,
  };
  MemoryQuery copyWith({
    int? page,
    int? pageSize,
    String? search,
    String? feeling,
    String? tag,
    DateTime? from,
    DateTime? to,
    int? year,
    int? month,
    MemorySort? sort,
    bool clearSearch = false,
    bool clearFeeling = false,
    bool clearTag = false,
    bool clearFrom = false,
    bool clearTo = false,
    bool clearYear = false,
    bool clearMonth = false,
  }) => MemoryQuery(
    page: page ?? this.page,
    pageSize: pageSize ?? this.pageSize,
    search: clearSearch ? null : search ?? this.search,
    feeling: clearFeeling ? null : feeling ?? this.feeling,
    tag: clearTag ? null : tag ?? this.tag,
    from: clearFrom ? null : from ?? this.from,
    to: clearTo ? null : to ?? this.to,
    year: clearYear ? null : year ?? this.year,
    month: clearMonth ? null : month ?? this.month,
    sort: sort ?? this.sort,
  );
  bool get hasFilters =>
      _hasText(search) ||
      _hasText(feeling) ||
      _hasText(tag) ||
      from != null ||
      to != null ||
      year != null ||
      month != null ||
      sort == MemorySort.oldest;
}

bool _hasText(String? value) => value != null && value.trim().isNotEmpty;

class MemoryPage {
  const MemoryPage({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalItems,
    required this.totalPages,
    required this.hasNextPage,
  });
  final List<MemoryListItem> items;
  final int page;
  final int pageSize;
  final int totalItems;
  final int totalPages;
  final bool hasNextPage;
  factory MemoryPage.fromJson(Map<String, dynamic> json) => MemoryPage(
    items: (json['items'] as List? ?? const [])
        .map(
          (item) =>
              MemoryListItem.fromJson(Map<String, dynamic>.from(item as Map)),
        )
        .toList(growable: false),
    page: json['page'] as int? ?? 1,
    pageSize: json['pageSize'] as int? ?? 20,
    totalItems: json['totalItems'] as int? ?? 0,
    totalPages: json['totalPages'] as int? ?? 0,
    hasNextPage: json['hasNextPage'] as bool? ?? false,
  );
}

class MemoryDraft {
  const MemoryDraft({
    required this.title,
    required this.story,
    required this.feeling,
    required this.memoryDate,
    required this.location,
    required this.tags,
  });
  final String title;
  final String? story;
  final String feeling;
  final DateTime memoryDate;
  final String? location;
  final List<String> tags;
  factory MemoryDraft.fromDetails(MemoryDetails details) => MemoryDraft(
    title: details.title,
    story: details.story,
    feeling: details.feeling,
    memoryDate: details.memoryDate,
    location: details.location,
    tags: details.tags,
  );
  Map<String, dynamic> toJson() => {
    'title': title.trim(),
    'story': _nullableTrim(story),
    'feeling': feeling,
    'memoryDate': formatMemoryDate(memoryDate),
    'location': _nullableTrim(location),
    'tags': tags
        .map((tag) => tag.trim())
        .where((tag) => tag.isNotEmpty)
        .toList(growable: false),
  };
}

String? _nullableTrim(String? value) {
  final trimmed = value?.trim();
  return trimmed == null || trimmed.isEmpty ? null : trimmed;
}
