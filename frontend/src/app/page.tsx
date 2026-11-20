import { redirect } from 'next/navigation';

/** El panel es la entrada principal de la aplicación. */
export default function Home() {
  redirect('/dashboard');
}
