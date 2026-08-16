import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../shared/toast/toast.service';
import { apiErrorMessage } from '../../shared/utils/api-error';
import { MobileSessionDto } from '../../core/models/mobile-auth.models';
import { PlanContextService } from '../../core/services/plan-context.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export class ProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly planContext = inject(PlanContextService);

  readonly loading = signal(true);
  readonly savingProfile = signal(false);
  readonly savingPassword = signal(false);
  readonly sessionsLoading = signal(false);
  readonly revokingSessionId = signal<string | null>(null);
  readonly mobileSessions = signal<MobileSessionDto[]>([]);
  readonly isMobileApp = this.auth.isMobileApp;

  readonly email = signal('');
  readonly savedName = signal('');

  displayName = '';
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';

  readonly initials = computed(() => {
    const name = this.savedName().trim();
    if (name) {
      const parts = name.split(/\s+/).filter(Boolean);
      if (parts.length >= 2) {
        return (parts[0]![0]! + parts[1]![0]!).toUpperCase();
      }
      return name.slice(0, 2).toUpperCase();
    }
    const mail = this.email();
    return mail ? mail.slice(0, 2).toUpperCase() : 'PD';
  });

  ngOnInit(): void {
    this.auth.me().subscribe({
      next: (me) => {
        this.email.set(me.email);
        this.savedName.set(me.displayName);
        this.displayName = me.displayName;
        this.loading.set(false);
        if (this.isMobileApp) {
          this.loadMobileSessions();
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.toast.error(apiErrorMessage(err, 'Profil yüklenemedi.'));
      },
    });
  }

  saveProfile(): void {
    const name = this.displayName.trim();
    if (!name) {
      this.toast.error('Görünen ad zorunlu.');
      return;
    }
    this.savingProfile.set(true);
    this.auth.updateProfile(name).subscribe({
      next: () => {
        this.savingProfile.set(false);
        this.displayName = name;
        this.savedName.set(name);
        this.toast.success('Profil güncellendi.');
      },
      error: (err) => {
        this.savingProfile.set(false);
        this.toast.error(apiErrorMessage(err, 'Profil güncellenemedi.'));
      },
    });
  }

  savePassword(): void {
    if (!this.currentPassword || !this.newPassword) {
      this.toast.error('Mevcut ve yeni şifre gerekli.');
      return;
    }
    if (this.newPassword.length < 10 || !/[A-Za-z]/.test(this.newPassword) || !/\d/.test(this.newPassword)) {
      this.toast.error('Yeni şifre en az 10 karakter; en az bir harf ve rakam içermeli.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.toast.error('Yeni şifre tekrarı eşleşmiyor.');
      return;
    }
    this.savingPassword.set(true);
    this.auth.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: () => {
        this.savingPassword.set(false);
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        this.toast.success('Şifre güncellendi.');
      },
      error: (err) => {
        this.savingPassword.set(false);
        this.toast.error(apiErrorMessage(err, 'Şifre güncellenemedi.'));
      },
    });
  }

  revokeSession(session: MobileSessionDto): void {
    if (session.isCurrent || this.revokingSessionId()) {
      return;
    }

    this.revokingSessionId.set(session.id);
    this.auth.revokeMobileSession(session.id).subscribe({
      next: () => {
        this.revokingSessionId.set(null);
        this.mobileSessions.update((sessions) => sessions.filter((item) => item.id !== session.id));
        this.toast.success('Cihaz oturumu kapatıldı.');
      },
      error: (err) => {
        this.revokingSessionId.set(null);
        this.toast.error(apiErrorMessage(err, 'Cihaz oturumu kapatılamadı.'));
      },
    });
  }

  logout(): void {
    this.planContext.clear();
    this.auth.logout('/');
  }

  sessionDate(value: string | null): string {
    if (!value) {
      return 'Henüz yenilenmedi';
    }
    return new Intl.DateTimeFormat('tr-TR', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }

  private loadMobileSessions(): void {
    this.sessionsLoading.set(true);
    this.auth.listMobileSessions().subscribe({
      next: (sessions) => {
        this.mobileSessions.set(sessions);
        this.sessionsLoading.set(false);
      },
      error: () => this.sessionsLoading.set(false),
    });
  }
}
