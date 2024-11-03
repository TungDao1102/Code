public class ObjectComparer<T, TKey>(Func<T, TKey> getId) : IEqualityComparer<T> where T : class where TKey : notnull
 {
     private readonly Func<T, TKey> _getId = getId ?? throw new ArgumentNullException(nameof(getId));

     public bool Equals(T? x, T? y)
     {
         if (x == null || y == null)
         {
             return false;
         }

         return EqualityComparer<TKey>.Default.Equals(_getId(x), _getId(y));
     }

     public int GetHashCode([DisallowNull] T obj)
     {
         return _getId(obj).GetHashCode();
     }
 }    
