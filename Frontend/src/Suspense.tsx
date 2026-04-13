import React, { Suspense as ReactSuspense } from 'react';

interface SuspenseProps {
  element: string;
}

export const Suspense = ({ element }: SuspenseProps) => {
  const LazyComponent = React.lazy(() => import(`./${element}`));

  return (
    <ReactSuspense>
      <LazyComponent />
    </ReactSuspense>
  );
};
