import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { MemberListComponent } from './member-list/member-list.component';
import { MessagesComponent } from './messages/messages.component';
import { ListsComponent } from './lists/lists.component';
import { AuthGuard } from './_guards/auth.guard';

// list of routes. remember the order is important, so put wildcard at the end
export const appRoutes: Routes = [
  {  path: '', component: HomeComponent }, // for root ulr, we are redirected to home component
  // for combined paaths, the toor is added to children one
  {
      path: '',
      runGuardsAndResolvers: 'always',
      canActivate: [AuthGuard],
      children: [
        {  path: 'members', component: MemberListComponent },
        {  path: 'messages', component: MessagesComponent },
        {  path: 'lists', component: ListsComponent },
      ]
  },
  {  path: '**', redirectTo: '', pathMatch: 'full' } // match the full path to be redirected to
];
