using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace TemplateEdit
{
public class MemoryImageCache
    {
        private readonly int _maxItems;
        private readonly ConcurrentDictionary<string, BitmapImage> _cache = new();
        private readonly LinkedList<string> _lru = new();
        private readonly object _lock = new();

        public MemoryImageCache(int maxItems = 20)
        {
            _maxItems = maxItems; // keep only 20 images in RAM
        }

        public BitmapImage Get(string key)
        {
            if (_cache.TryGetValue(key, out var image))
            {
                lock (_lock)
                {
                    _lru.Remove(key);
                    _lru.AddFirst(key);
                }
                return image;
            }

            return null;
        }

        public void Add(string key, BitmapImage image)
        {
            lock (_lock)
            {
                if (_cache.ContainsKey(key))
                    return;

                if (_cache.Count >= _maxItems)
                {
                    string lastKey = _lru.Last.Value;
                    _lru.RemoveLast();
                    _cache.TryRemove(lastKey, out _);
                }

                _cache[key] = image;
                _lru.AddFirst(key);
            }
        }
    }
}

