 public static IQueryable<TTarget> SelectSpecificFields<TSource, TTarget>(this IQueryable<TSource> source)
        {
            var targetType = typeof(TTarget);
            var sourceType = typeof(TSource);

            var parameter = Expression.Parameter(sourceType, "x");
            var bindings = targetType.GetProperties()
                .Select(p =>
                {
                    var sourceProperty = sourceType.GetProperty(p.Name);
                    return sourceProperty != null
                        ? Expression.Bind(p, Expression.Property(parameter, sourceProperty))
                        : null;
                })
                .Where(binding => binding != null)
                .OfType<MemberBinding>();

            var selector = Expression.Lambda<Func<TSource, TTarget>>(
                Expression.MemberInit(Expression.New(targetType), bindings), parameter);

            return source.Select(selector);
        }
