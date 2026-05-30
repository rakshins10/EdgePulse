import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { IngestionModule } from './ingestion/ingestion.module';
import { MessagingModule } from './messaging/messaging.module';

@Module({
  imports: [
    // Load .env file and make ConfigService available app-wide
    ConfigModule.forRoot({ isGlobal: true }),
    MessagingModule,
    IngestionModule,
  ],
})
export class AppModule {}
